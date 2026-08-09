using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Models.Learning;
using LearningAssistant.Data.Database;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Common.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace LearningAssistant.Tests
{
    public class NoteServiceTests : IDisposable
    {
        private readonly Mock<IDbContextFactory<AppDbContext>> _mockDbContextFactory;
        private readonly Mock<IDataPersistenceService> _mockPersistence;
        private readonly Mock<ILogger<NoteService>> _mockLogger;
        private readonly Mock<IEventBus> _mockEventBus;
        private NoteService? _service;
        private DbContextOptions<AppDbContext>? _options;

        public NoteServiceTests()
        {
            _mockDbContextFactory = new Mock<IDbContextFactory<AppDbContext>>();
            _mockDbContextFactory.Setup(f => f.CreateDbContext())
                .Returns(() => new AppDbContext());

            _mockPersistence = new Mock<IDataPersistenceService>();
            _mockLogger = new Mock<ILogger<NoteService>>();
            _mockEventBus = new Mock<IEventBus>();
        }

        public void Dispose()
        {
            _service = null;
        }

        private NoteService CreateService()
        {
            return new NoteService(
                _mockDbContextFactory.Object,
                _mockLogger.Object,
                _mockPersistence.Object,
                _mockEventBus.Object);
        }

        [Fact]
        public void Constructor_WithNullDbContextFactory_ShouldThrow()
        {
            Action act = () => new NoteService(
                null!,
                _mockLogger.Object,
                _mockPersistence.Object,
                _mockEventBus.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("dbContextFactory");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Action act = () => new NoteService(
                _mockDbContextFactory.Object,
                null!,
                _mockPersistence.Object,
                _mockEventBus.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithNullPersistenceService_ShouldThrow()
        {
            Action act = () => new NoteService(
                _mockDbContextFactory.Object,
                _mockLogger.Object,
                null!,
                _mockEventBus.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("persistenceService");
        }

        [Fact]
        public void AddNote_ShouldAddNoteWithGeneratedId()
        {
            _service = CreateService();
            var note = new NoteItem
            {
                Title = "Test Note",
                Content = "Test Content",
                Category = "TestCategory"
            };

            _service.AddNote("test_user", note);

            note.Id.Should().NotBeNullOrEmpty();
            note.UserId.Should().Be("test_user");
            note.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            note.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void AddNote_ShouldPublishNoteAddedEvent()
        {
            _service = CreateService();
            var note = new NoteItem
            {
                Title = "Test Note",
                Content = "Test Content"
            };

            _service.AddNote("test_user", note);

            _mockEventBus.Verify(e => e.Publish(It.IsAny<NoteAddedEvent>()), Times.Once);
        }

        [Fact]
        public void UpdateNote_ShouldUpdateExistingNote()
        {
            _service = CreateService();
            var note = new NoteItem
            {
                Title = "Original Title",
                Content = "Original Content",
                Category = "Category"
            };
            _service.AddNote("test_user", note);

            note.Title = "Updated Title";
            note.Content = "Updated Content";
            _service.UpdateNote("test_user", note);

            var updatedNote = _service.GetNote("test_user", note.Id);
            updatedNote.Should().NotBeNull();
            updatedNote!.Title.Should().Be("Updated Title");
            updatedNote.Content.Should().Be("Updated Content");
        }

        [Fact]
        public void UpdateNote_WithNonExistentId_ShouldNotThrow()
        {
            _service = CreateService();
            var note = new NoteItem
            {
                Id = "non_existent_id",
                Title = "Non Existent",
                Content = "Content"
            };

            Action act = () => _service.UpdateNote("test_user", note);

            act.Should().NotThrow();
        }

        [Fact]
        public void DeleteNote_ShouldRemoveNote()
        {
            _service = CreateService();
            var note = new NoteItem
            {
                Title = "To Delete",
                Content = "Content"
            };
            _service.AddNote("test_user", note);

            _service.DeleteNote("test_user", note.Id);

            var result = _service.GetNote("test_user", note.Id);
            result.Should().BeNull();
        }

        [Fact]
        public void DeleteNote_WithNonExistentId_ShouldNotThrow()
        {
            _service = CreateService();

            Action act = () => _service.DeleteNote("test_user", "non_existent_id");

            act.Should().NotThrow();
        }

        [Fact]
        public void GetNote_WithNonExistentId_ShouldReturnNull()
        {
            _service = CreateService();

            var result = _service.GetNote("test_user", "non_existent_id");

            result.Should().BeNull();
        }

        [Fact]
        public void GetNotes_ShouldReturnAllNotes()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Note 1", Content = "Content 1" });
            _service.AddNote("test_user", new NoteItem { Title = "Note 2", Content = "Content 2" });

            var notes = _service.GetNotes("test_user");

            notes.Should().HaveCount(2);
        }

        [Fact]
        public void GetNotes_WithCategoryFilter_ShouldReturnFilteredNotes()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Math Note", Content = "Content", Category = "Math" });
            _service.AddNote("test_user", new NoteItem { Title = "English Note", Content = "Content", Category = "English" });

            var notes = _service.GetNotes("test_user", category: "Math");

            notes.Should().HaveCount(1);
            notes[0].Category.Should().Be("Math");
        }

        [Fact]
        public void GetNotes_WithTagFilter_ShouldReturnFilteredNotes()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Note 1", Content = "Content", Tags = new List<string> { "tag1", "tag2" } });
            _service.AddNote("test_user", new NoteItem { Title = "Note 2", Content = "Content", Tags = new List<string> { "tag3" } });

            var notes = _service.GetNotes("test_user", tag: "tag1");

            notes.Should().HaveCount(1);
            notes[0].Title.Should().Be("Note 1");
        }

        [Fact]
        public void SearchNotes_WithKeyword_ShouldReturnMatchingNotes()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Apple Note", Content = "Apple content" });
            _service.AddNote("test_user", new NoteItem { Title = "Banana Note", Content = "Banana content" });

            var notes = _service.SearchNotes("test_user", "Apple");

            notes.Should().HaveCount(1);
            notes[0].Title.Should().Be("Apple Note");
        }

        [Fact]
        public void SearchNotes_WithEmptyKeyword_ShouldReturnEmpty()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Test Note", Content = "Content" });

            var notes = _service.SearchNotes("test_user", "");

            notes.Should().BeEmpty();
        }

        [Fact]
        public void GetRelatedNotes_ShouldReturnRelatedNotes()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Related Note", Content = "Content", RelatedType = "Word", RelatedItemId = "word_123" });
            _service.AddNote("test_user", new NoteItem { Title = "Unrelated Note", Content = "Content", RelatedType = "Word", RelatedItemId = "word_456" });

            var notes = _service.GetRelatedNotes("test_user", "Word", "word_123");

            notes.Should().HaveCount(1);
            notes[0].Title.Should().Be("Related Note");
        }

        [Fact]
        public void SetFavorite_ShouldUpdateFavoriteStatus()
        {
            _service = CreateService();
            var note = new NoteItem { Title = "Test Note", Content = "Content", IsFavorite = false };
            _service.AddNote("test_user", note);

            _service.SetFavorite("test_user", note.Id, true);

            var updatedNote = _service.GetNote("test_user", note.Id);
            updatedNote.Should().NotBeNull();
            updatedNote!.IsFavorite.Should().Be(true);
        }

        [Fact]
        public void GetFavoriteNotes_ShouldReturnOnlyFavorites()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Favorite Note", Content = "Content", IsFavorite = true });
            _service.AddNote("test_user", new NoteItem { Title = "Non Favorite Note", Content = "Content", IsFavorite = false });

            var favorites = _service.GetFavoriteNotes("test_user");

            favorites.Should().HaveCount(1);
            favorites[0].Title.Should().Be("Favorite Note");
        }

        [Fact]
        public void GetAllCategories_ShouldReturnDistinctCategories()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Note 1", Content = "Content", Category = "Math" });
            _service.AddNote("test_user", new NoteItem { Title = "Note 2", Content = "Content", Category = "Math" });
            _service.AddNote("test_user", new NoteItem { Title = "Note 3", Content = "Content", Category = "English" });

            var categories = _service.GetAllCategories("test_user");

            categories.Should().HaveCount(2);
            categories.Should().Contain("Math", "English");
        }

        [Fact]
        public void GetAllTags_ShouldReturnDistinctTags()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Note 1", Content = "Content", Tags = new List<string> { "tag1", "tag2" } });
            _service.AddNote("test_user", new NoteItem { Title = "Note 2", Content = "Content", Tags = new List<string> { "tag2", "tag3" } });

            var tags = _service.GetAllTags("test_user");

            tags.Should().HaveCount(3);
            tags.Should().Contain("tag1", "tag2", "tag3");
        }

        [Fact]
        public void MarkAsReviewed_ShouldUpdateReviewCount()
        {
            _service = CreateService();
            var note = new NoteItem { Title = "Test Note", Content = "Content", ReviewCount = 0 };
            _service.AddNote("test_user", note);

            _service.MarkAsReviewed("test_user", note.Id);

            var updatedNote = _service.GetNote("test_user", note.Id);
            updatedNote.Should().NotBeNull();
            updatedNote!.ReviewCount.Should().Be(1);
            updatedNote.LastReviewedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void GetNotesForReview_ShouldReturnNotesNotReviewedRecently()
        {
            _service = CreateService();
            var oldNote = new NoteItem { Title = "Old Note", Content = "Content", LastReviewedAt = DateTime.Now.AddDays(-10) };
            var recentNote = new NoteItem { Title = "Recent Note", Content = "Content", LastReviewedAt = DateTime.Now.AddDays(-1) };
            _service.AddNote("test_user", oldNote);
            _service.AddNote("test_user", recentNote);

            var notes = _service.GetNotesForReview("test_user", 7);

            notes.Should().HaveCount(1);
            notes[0].Title.Should().Be("Old Note");
        }

        [Fact]
        public void GetNoteCount_ShouldReturnTotalCount()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Note 1", Content = "Content" });
            _service.AddNote("test_user", new NoteItem { Title = "Note 2", Content = "Content" });

            var count = _service.GetNoteCount("test_user");

            count.Should().Be(2);
        }

        [Fact]
        public void ExportNotes_ShouldCreateFile()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "Export Test", Content = "Export Content" });

            var tempPath = Path.Combine(Path.GetTempPath(), $"notes_export_{Guid.NewGuid()}.txt");
            _service.ExportNotes("test_user", tempPath);

            File.Exists(tempPath).Should().BeTrue();
            File.ReadAllText(tempPath).Should().Contain("Export Test");
            File.Delete(tempPath);
        }

        [Fact]
        public void ExportNotes_AsMarkdown_ShouldCreateMarkdownFile()
        {
            _service = CreateService();
            _service.AddNote("test_user", new NoteItem { Title = "MD Test", Content = "MD Content" });

            var tempPath = Path.Combine(Path.GetTempPath(), $"notes_export_{Guid.NewGuid()}.md");
            _service.ExportNotes("test_user", tempPath, "md");

            File.Exists(tempPath).Should().BeTrue();
            File.ReadAllText(tempPath).Should().Contain("# 学习笔记导出");
            File.Delete(tempPath);
        }

        [Fact]
        public void GetNotesPaged_ShouldReturnPagedResults()
        {
            _service = CreateService();
            for (int i = 1; i <= 5; i++)
            {
                _service.AddNote("test_user", new NoteItem { Title = $"Note {i}", Content = "Content" });
            }

            var (items, total) = _service.GetNotesPaged("test_user", 1, 2);

            items.Should().HaveCount(2);
            total.Should().Be(5);
        }

        [Fact]
        public void BatchDelete_ShouldDeleteMultipleNotes()
        {
            _service = CreateService();
            var note1 = new NoteItem { Title = "Note 1", Content = "Content" };
            var note2 = new NoteItem { Title = "Note 2", Content = "Content" };
            _service.AddNote("test_user", note1);
            _service.AddNote("test_user", note2);

            _service.BatchDelete("test_user", new List<string> { note1.Id, note2.Id });

            var notes = _service.GetNotes("test_user");
            notes.Should().BeEmpty();
        }

        [Fact]
        public void BatchMove_ShouldMoveNotesToCategory()
        {
            _service = CreateService();
            var note1 = new NoteItem { Title = "Note 1", Content = "Content", Category = "Old" };
            var note2 = new NoteItem { Title = "Note 2", Content = "Content", Category = "Old" };
            _service.AddNote("test_user", note1);
            _service.AddNote("test_user", note2);

            _service.BatchMove("test_user", new List<string> { note1.Id, note2.Id }, "NewCategory");

            var notes = _service.GetNotes("test_user", "NewCategory");
            notes.Should().HaveCount(2);
        }

        [Fact]
        public void BatchDelete_WithEmptyIds_ShouldNotThrow()
        {
            _service = CreateService();

            Action act = () => _service.BatchDelete("test_user", new List<string>());

            act.Should().NotThrow();
        }

        [Fact]
        public void BatchMove_WithEmptyIds_ShouldNotThrow()
        {
            _service = CreateService();

            Action act = () => _service.BatchMove("test_user", new List<string>(), "Category");

            act.Should().NotThrow();
        }
    }
}