using Domain.Models;

namespace Infrastructure.Seed
{
    public static class QuestionsJsonSeeder
    {
        public static List<QuestionsJson> Seed()
        {
            // EF Core seed values must be deterministic so unrelated migrations do not update this record.
            var now = new DateTime(2026, 7, 25, 15, 2, 6, 115, DateTimeKind.Utc).AddTicks(5024);

            var payload =
                """
                {
                    "Questions": [
                        {
                            "Type": 0,
                            "MCQ": {
                                "Prompt": "What is 2+2?",
                                "Choices": [ "3", "4", "5", "6" ],
                                "CorrectIndex": 1,
                                "Timer": 8.0
                            }
                        },
                        {
                            "Type": 1,
                            "Ordering": {
                                "Prompt": "Arrange numbers",
                                "Items": [ "5", "3", "2", "4", "6" ],
                                "CorrectOrder": [ 3, 1, 0, 2, 4 ],
                                "Timer": 10.0
                            }
                        },
                        {
                            "Type": 3,
                            "FillBlank": {
                                "Prompt": "What color is an apple?",
                                "CorrectAnswers": [ "red", "green", "yellow" ],
                                "Timer": 8.0
                            }
                        },
                        {
                            "Type": 6,
                            "TrueOrFalse": {
                                "Prompt": "Can birds fly?",
                                "Answer": true,
                                "Timer": 8.0
                            }
                        },
                        {
                            "Type": 5,
                            "DragAndDrop": {
                                "Prompt": "The lion eats @, and the cow gives us @",
                                "Spaces": 2,
                                "Answers": [ "milk", "apples", "meat", "cheese" ],
                                "CorrectAnswers": [ "meat", "milk" ],
                                "Timer": 10.0
                            }
                        }
                    ]
                }
                """;

            return new List<QuestionsJson>
            {
                new QuestionsJson
                {
                    Id = 1,
                    Grade = 5,
                    Assignment = 1,
                    Version = 1,
                    PayloadJson = payload,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            };
        }
    }
}
