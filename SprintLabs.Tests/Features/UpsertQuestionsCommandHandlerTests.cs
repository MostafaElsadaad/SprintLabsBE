using System;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Features.Questions;
using Domain.Models;
using Domain.Repositories;
using FluentAssertions;
using Moq;
using Shared.Exceptions;
using Shared.Enums;
using Xunit;

namespace Compass.Tests.Features
{
    public class UpsertQuestionsCommandHandlerTests
    {
        private readonly Mock<IBaseRepository<QuestionsJson>> _repositoryMock;
        private readonly UpsertQuestionsCommandHandler _handler;

        public UpsertQuestionsCommandHandlerTests()
        {
            _repositoryMock = new Mock<IBaseRepository<QuestionsJson>>();
            _handler = new UpsertQuestionsCommandHandler(_repositoryMock.Object);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(21)]
        public async Task Handle_InvalidGrade_ThrowsGenericException(int grade)
        {
            // Arrange
            var command = new UpsertQuestionsCommand
            {
                Grade = grade,
                Assignment = 1,
                PayloadJson = JsonDocument.Parse("{\"Questions\": [{\"Prompt\": \"Test\"}]}").RootElement
            };

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            var exception = await act.Should().ThrowAsync<GenericException>();
            exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Which.ErrorCode.Should().Be(ErrorCode.Failure);
            exception.Which.Message.Should().Be(ErrorMessage.InvalidInput);
        }

        [Fact]
        public async Task Handle_InvalidPayloadJsonValueKind_ThrowsGenericException()
        {
            // Arrange
            // PayloadJson is not an object, but a string/array/etc
            var command = new UpsertQuestionsCommand
            {
                Grade = 5,
                Assignment = 1,
                PayloadJson = JsonDocument.Parse("[]").RootElement
            };

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            var exception = await act.Should().ThrowAsync<GenericException>();
            exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Which.Message.Should().Be(ErrorMessage.InvalidInput);
        }

        [Fact]
        public async Task Handle_MissingQuestionsProperty_ThrowsGenericException()
        {
            // Arrange
            var command = new UpsertQuestionsCommand
            {
                Grade = 5,
                Assignment = 1,
                PayloadJson = JsonDocument.Parse("{\"NotQuestions\": []}").RootElement
            };

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            var exception = await act.Should().ThrowAsync<GenericException>();
            exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Which.Message.Should().Be(ErrorMessage.InvalidInput);
        }

        [Fact]
        public async Task Handle_EmptyQuestionsArray_ThrowsGenericException()
        {
            // Arrange
            var command = new UpsertQuestionsCommand
            {
                Grade = 5,
                Assignment = 1,
                PayloadJson = JsonDocument.Parse("{\"Questions\": []}").RootElement
            };

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            var exception = await act.Should().ThrowAsync<GenericException>();
            exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Which.Message.Should().Be(ErrorMessage.InvalidInput);
        }

        [Fact]
        public async Task Handle_ValidNewGrade_InsertsRecord()
        {
            // Arrange
            var payload = JsonDocument.Parse("{\"Questions\": [{\"Prompt\": \"Test\"}]}").RootElement;
            var command = new UpsertQuestionsCommand
            {
                Grade = 10,
                Assignment = 2,
                PayloadJson = payload
            };

            _repositoryMock
                .Setup(r => r.GetByCustomConditionAsync(It.IsAny<Expression<Func<QuestionsJson, bool>>>()))
                .ReturnsAsync((QuestionsJson)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(r => r.AddAsync(It.Is<QuestionsJson>(q =>
                q.Grade == 10 &&
                q.Assignment == 2 &&
                q.Version == 1
            )), Times.Once);

            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);

            result.Grade.Should().Be(10);
            result.Assignment.Should().Be(2);
            result.Version.Should().Be(1);
            result.PayloadJson.GetProperty("Questions").GetArrayLength().Should().Be(1);
        }

        [Fact]
        public async Task Handle_ValidExistingGrade_UpdatesRecord()
        {
            // Arrange
            var existingPayload = "{\"Questions\": [{\"Prompt\": \"Old\"}]}";
            var existingQuestion = new QuestionsJson
            {
                Id = 1,
                Grade = 10,
                Assignment = 1,
                PayloadJson = existingPayload,
                Version = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var newPayload = JsonDocument.Parse("{\"Questions\": [{\"Prompt\": \"New\"}]}").RootElement;
            var command = new UpsertQuestionsCommand
            {
                Grade = 10,
                Assignment = 2,
                PayloadJson = newPayload
            };

            _repositoryMock
                .Setup(r => r.GetByCustomConditionAsync(It.IsAny<Expression<Func<QuestionsJson, bool>>>()))
                .ReturnsAsync(existingQuestion);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<QuestionsJson>(q =>
                q.Id == 1 &&
                q.Grade == 10 &&
                q.Assignment == 2 &&
                q.Version == 3
            )), Times.Once);

            _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);

            result.Grade.Should().Be(10);
            result.Assignment.Should().Be(2);
            result.Version.Should().Be(3);
            result.PayloadJson.GetProperty("Questions").GetArrayLength().Should().Be(1);
        }
    }
}
