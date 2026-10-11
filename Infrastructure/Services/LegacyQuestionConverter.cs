using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using Domain.Models;
using Shared.Requests.QuestionData;

namespace Infrastructure.Services;

internal static class LegacyQuestionConverter
{
    // Stable namespace for this one-time legacy migration tool; export these IDs, do not regenerate elsewhere.
    private static readonly Guid NamespaceId = Guid.Parse("f8b557f4-a6f4-4d45-9697-0f738acf3b15");

    public static List<QuestionDataRequest> Convert(QuestionsJson pack)
    {
        if (pack.PayloadJson.Length > 16 * 1024 * 1024) throw QuestionDataService.Error("Legacy conversion is limited to 16 MiB of JSON per pack.");
        try
        {
            using var json = JsonDocument.Parse(pack.PayloadJson);
            var source = json.RootElement.GetProperty("Questions");
            if (source.ValueKind != JsonValueKind.Array || source.GetArrayLength() is < 1 or > 2000)
                throw QuestionDataService.Error("Legacy packs must contain 1..2000 questions.");
            var result = source.EnumerateArray().Select((item, index) => ConvertQuestion(pack, item, index)).ToList();
            if (result.Select(x => x.QuestionId).Distinct().Count() != result.Count)
                throw QuestionDataService.Error("Legacy question IDs must be distinct.");
            foreach (var q in result) QuestionDataValidation.Question(q);
            return result;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            throw QuestionDataService.Error("Legacy payload is malformed or uses an unsupported answer structure.");
        }
    }

    private static QuestionDataRequest ConvertQuestion(QuestionsJson pack, JsonElement item, int index)
    {
        var type = item.GetProperty("Type").GetInt32();
        var name = type switch { 0 => "MCQ", 1 => "Ordering", 2 => "MatchingPairs", 3 => "FillBlank", 5 => "DragAndDrop", 6 => "TrueOrFalse",
            _ => throw QuestionDataService.Error("Legacy ImageMCQ or unknown types cannot be converted by this schema.") };
        var data = item.GetProperty(name);
        Guid id;
        if (item.TryGetProperty("QuestionId", out var existing))
        {
            if (existing.ValueKind != JsonValueKind.String || !Guid.TryParse(existing.GetString(), out id) || id == Guid.Empty)
                throw QuestionDataService.Error("Existing QuestionId must be a nonempty UUID.");
        }
        else
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream)) Canonical(writer, item);
            id = StableId($"legacy-v1:{pack.Id}:{pack.Grade}:{pack.Assignment}:{index}:{Encoding.UTF8.GetString(stream.ToArray())}");
        }
        var q = new QuestionDataRequest { QuestionId = id, Curriculum = "DUMMY", Language = "und", Subject = "DUMMY",
            Grade = pack.Grade, Term = 1, Unit = 1, Lesson = 1, QuestionType = name,
            QuestionText = data.GetProperty("Prompt").GetString()!,
            TimerSeconds = data.TryGetProperty("Timer", out var timer) ? timer.GetDecimal() : 10m };
        string Child(string kind, int position) => $"{id:N}:{kind}:{position:D3}";
        switch (type)
        {
            case 0:
                var choices = Strings(data, "Choices"); var correct = data.GetProperty("CorrectIndex").GetInt32();
                if (correct < 0 || correct >= choices.Count) throw QuestionDataService.Error("Invalid legacy MCQ correct index.");
                q.Choices = choices.Select((text, i) => new QuestionChoiceData { ChoiceId = Child("c", i), ChoiceText = text, IsCorrect = i == correct }).ToList();
                break;
            case 1:
                var items = Strings(data, "Items"); var order = data.GetProperty("CorrectOrder").EnumerateArray().ToList();
                if (items.Count != order.Count) throw QuestionDataService.Error("Ordering requires one correct position per item.");
                List<int> indices;
                if (order.All(x => x.ValueKind == JsonValueKind.Number)) indices = order.Select(x => x.GetInt32()).ToList();
                else if (order.All(x => x.ValueKind == JsonValueKind.String))
                {
                    var unused = Enumerable.Range(0, items.Count).ToList(); indices = new();
                    foreach (var value in order)
                    {
                        var found = unused.FindIndex(i => items[i] == value.GetString());
                        if (found < 0) throw QuestionDataService.Error("Correct order must contain every item instance.");
                        indices.Add(unused[found]); unused.RemoveAt(found);
                    }
                }
                else throw QuestionDataService.Error("Correct order cannot mix indices and text.");
                QuestionDataValidation.Positions(indices, items.Count);
                q.Items = items.Select((text, i) => new QuestionOrderingData { ItemId = Child("i", i), ItemText = text, CorrectPosition = indices.IndexOf(i) }).ToList();
                break;
            case 2:
                var left = Strings(data, "LeftItems"); var right = Strings(data, "RightItems");
                if (left.Count != right.Count) throw QuestionDataService.Error("Matching columns must contain equal numbers of items.");
                q.Pairs = left.Select((text, i) => new QuestionPairData { PairId = Child("p", i), LeftText = text, RightText = right[i] }).ToList();
                break;
            case 3: q.AcceptedAnswers = Strings(data, "CorrectAnswers"); break;
            case 5:
                var options = Strings(data, "Answers"); var answers = Strings(data, "CorrectAnswers");
                if (data.GetProperty("Spaces").GetInt32() != answers.Count) throw QuestionDataService.Error("Blank count and answers do not match.");
                q.Options = options.Select((text, i) => new QuestionOptionData { OptionId = Child("o", i), OptionText = text }).ToList();
                q.BlankAnswers = answers.Select((text, i) => new QuestionBlankData { BlankNumber = i, CorrectText = text }).ToList();
                break;
            case 6:
                q.CorrectAnswer = data.TryGetProperty("CorrectAnswer", out var boolean) ? boolean.GetBoolean() : data.GetProperty("Answer").GetBoolean();
                break;
        }
        return q;
    }

    public static JsonElement Project(QuestionsJson pack, IReadOnlyList<QuestionDataRequest> questions)
    {
        var root = JsonNode.Parse(pack.PayloadJson)!;
        var source = root["Questions"]!.AsArray();
        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i]; var item = source[i]!; var data = item[q.QuestionType]!;
            item["QuestionId"] = q.QuestionId.ToString();
            data["Prompt"] = q.QuestionText; data["Timer"] = q.TimerSeconds;
            switch (q.QuestionType)
            {
                case "MCQ":
                    data["Choices"] = JsonSerializer.SerializeToNode(q.Choices.Select(x => x.ChoiceText));
                    data["CorrectIndex"] = q.Choices.FindIndex(x => x.IsCorrect == true);
                    data["ChoiceIds"] = JsonSerializer.SerializeToNode(q.Choices.Select(x => x.ChoiceId)); break;
                case "Ordering":
                    data["Items"] = JsonSerializer.SerializeToNode(q.Items.Select(x => x.ItemText));
                    data["CorrectOrder"] = JsonSerializer.SerializeToNode(q.Items.OrderBy(x => x.CorrectPosition).Select(x => x.ItemText));
                    data["ItemIds"] = JsonSerializer.SerializeToNode(q.Items.Select(x => x.ItemId)); break;
                case "MatchingPairs":
                    data["LeftItems"] = JsonSerializer.SerializeToNode(q.Pairs.Select(x => x.LeftText));
                    data["RightItems"] = JsonSerializer.SerializeToNode(q.Pairs.Select(x => x.RightText));
                    data["PairIds"] = JsonSerializer.SerializeToNode(q.Pairs.Select(x => x.PairId)); break;
                case "FillBlank": data["CorrectAnswers"] = JsonSerializer.SerializeToNode(q.AcceptedAnswers); break;
                case "DragAndDrop":
                    data["Spaces"] = q.BlankAnswers.Count;
                    data["Answers"] = JsonSerializer.SerializeToNode(q.Options.Select(x => x.OptionText));
                    data["CorrectAnswers"] = JsonSerializer.SerializeToNode(q.BlankAnswers.Select(x => x.CorrectText));
                    data["OptionIds"] = JsonSerializer.SerializeToNode(q.Options.Select(x => x.OptionId)); break;
                case "TrueOrFalse":
                    data["CorrectAnswer"] = q.CorrectAnswer;
                    if (data.AsObject().ContainsKey("Answer")) data["Answer"] = q.CorrectAnswer;
                    break;
            }
        }
        return JsonSerializer.SerializeToElement(root);
    }

    public static JsonElement ProjectBank(IReadOnlyList<QuestionDataRequest> questions)
    {
        var items = new JsonArray();
        foreach (var q in questions)
        {
            var type = q.QuestionType switch { "MCQ" => 0, "Ordering" => 1, "MatchingPairs" => 2, "FillBlank" => 3, "DragAndDrop" => 5, "TrueOrFalse" => 6,
                _ => throw QuestionDataService.Error("Unsupported compatible question type.") };
            items.Add(new JsonObject { ["Type"] = type, [q.QuestionType] = new JsonObject() });
        }
        return Project(new QuestionsJson { PayloadJson = new JsonObject { ["Questions"] = items }.ToJsonString() }, questions);
    }

    private static List<string> Strings(JsonElement data, string name)
        => data.GetProperty(name).EnumerateArray().Select(x => x.GetString()!).ToList();

    private static Guid StableId(string value)
    {
        var bytes = SHA1.HashData(NamespaceId.ToByteArray(true).Concat(Encoding.UTF8.GetBytes(value)).ToArray());
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x50); bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return new Guid(bytes.AsSpan(0, 16), true);
    }

    private static void Canonical(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var p in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal)) { writer.WritePropertyName(p.Name); Canonical(writer, p.Value); }
            writer.WriteEndObject();
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray(); foreach (var x in value.EnumerateArray()) Canonical(writer, x); writer.WriteEndArray();
        }
        else if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            writer.WriteRawValue(number.ToString("G29", CultureInfo.InvariantCulture));
        else value.WriteTo(writer);
    }
}
