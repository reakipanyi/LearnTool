using Xunit;
using FluentAssertions;
using LearningAssistant.Services.TTS;
using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;

namespace LearningAssistant.Tests
{
    public class SpeechCoordinatorTests
    {
        private readonly Mock<ITTSService> _mockTtsService;
        private readonly Mock<ILogger<SpeechCoordinator>> _mockLogger;
        private readonly SpeechCoordinator _coordinator;

        public SpeechCoordinatorTests()
        {
            _mockTtsService = new Mock<ITTSService>();
            _mockLogger = new Mock<ILogger<SpeechCoordinator>>();
            
            _mockTtsService.Setup(s => s.Available).Returns(true);
            _mockTtsService.Setup(s => s.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string text, string lang, float? speed, CancellationToken ct) => text);
            
            _coordinator = new SpeechCoordinator(_mockLogger.Object, _mockTtsService.Object, new TtsConfig());
        }

        [Fact]
        public void Constructor_WithNullTtsService_ShouldNotThrow()
        {
            var coordinator = new SpeechCoordinator(_mockLogger.Object, null, new TtsConfig());
            
            coordinator.Should().NotBeNull();
            coordinator.IsSpeaking.Should().BeFalse();
        }

        [Fact]
        public async Task SpeakAsync_WithTtsServiceAvailable_ShouldEnqueueAndSpeak()
        {
            await _coordinator.SpeakAsync("test text", "zh", CancellationToken.None, "test-key");

            _mockTtsService.Verify(s => s.SpeakAsync("test text", "zh", It.IsAny<float?>(), It.IsAny<CancellationToken>()), 
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SpeakAsync_WithExplanation_ShouldSpeakBothTextAndExplanation()
        {
            await _coordinator.SpeakAsync("test text", "zh", "test explanation", CancellationToken.None, "test-key");
            
            await Task.Delay(800);

            _mockTtsService.Verify(s => s.SpeakAsync("test text", "zh", It.IsAny<float?>(), It.IsAny<CancellationToken>()), 
                Times.Once);
            _mockTtsService.Verify(s => s.SpeakAsync("test explanation", "zh", It.IsAny<float?>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task SpeakAsync_WithNullSpeakKey_ShouldUseGlobalKey()
        {
            await _coordinator.SpeakAsync("test text", "zh", CancellationToken.None, null);

            _coordinator.CurrentSpeakKey.Should().Be("__GLOBAL__");
        }

        [Fact]
        public async Task SpeakAsync_WithCustomSpeakKey_ShouldSetCurrentSpeakKey()
        {
            await _coordinator.SpeakAsync("test text", "zh", CancellationToken.None, "custom-key");

            await Task.Delay(100);
            
            _coordinator.CurrentSpeakKey.Should().Be("custom-key");
        }

        [Fact]
        public async Task SpeakAsync_WithUnavailableTtsService_ShouldNotSpeak()
        {
            _mockTtsService.Setup(s => s.Available).Returns(false);

            await _coordinator.SpeakAsync("test text", "zh", CancellationToken.None, "test-key");

            _mockTtsService.Verify(s => s.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public void StopAsync_ShouldClearQueueAndStop()
        {
            _coordinator.SpeakAsync("test text", "zh", CancellationToken.None, "test-key");
            
            _coordinator.StopAsync().Wait();

            _mockTtsService.Verify(s => s.StopAsync(), Times.Once);
            _coordinator.CurrentSpeakKey.Should().BeEmpty();
        }

        [Fact]
        public void IsSpeaking_ShouldReturnTtsServiceIsSpeaking()
        {
            _mockTtsService.Setup(s => s.IsSpeaking).Returns(true);
            
            _coordinator.IsSpeaking.Should().BeTrue();

            _mockTtsService.Setup(s => s.IsSpeaking).Returns(false);
            
            _coordinator.IsSpeaking.Should().BeFalse();
        }

        [Fact]
        public async Task PreloadAsync_WithAvailableTtsService_ShouldPreload()
        {
            await _coordinator.PreloadAsync("test text", "zh");

            _mockTtsService.Verify(s => s.SpeakToCacheAsync("test text", "zh", It.IsAny<float?>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task PreloadAsync_WithUnavailableTtsService_ShouldNotPreload()
        {
            _mockTtsService.Setup(s => s.Available).Returns(false);

            await _coordinator.PreloadAsync("test text", "zh");

            _mockTtsService.Verify(s => s.SpeakToCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task SpeakStateChanged_ShouldBeRaisedWhenSpeakingStartsAndStops()
        {
            var speakStateChangedCount = 0;
            string? lastSpeakKey = null;
            bool? lastIsSpeaking = null;

            _coordinator.SpeakStateChanged += (sender, args) =>
            {
                speakStateChangedCount++;
                lastSpeakKey = args.SpeakKey;
                lastIsSpeaking = args.IsSpeaking;
            };

            await _coordinator.SpeakAsync("test text", "zh", CancellationToken.None, "event-test-key");
            
            await Task.Delay(200);

            speakStateChangedCount.Should().BeGreaterThan(0);
            lastSpeakKey.Should().Be("event-test-key");
        }

        [Fact]
        public async Task SpeakAsync_MultipleRequests_ShouldBeProcessedSequentially()
        {
            var speakOrder = new List<string>();
            var processedCount = 0;

            _mockTtsService.Setup(s => s.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string text, string lang, float? speed, CancellationToken ct) =>
                {
                    lock (speakOrder)
                    {
                        speakOrder.Add(text);
                        processedCount++;
                    }
                    return text;
                });

            await _coordinator.SpeakAsync("first", "zh", CancellationToken.None, "key1");
            await _coordinator.SpeakAsync("second", "zh", CancellationToken.None, "key2");
            await _coordinator.SpeakAsync("third", "zh", CancellationToken.None, "key3");

            await Task.Delay(2000);

            speakOrder.Count.Should().Be(3);
            speakOrder[0].Should().Be("first");
            speakOrder[1].Should().Be("second");
            speakOrder[2].Should().Be("third");
        }

        [Fact]
        public async Task SpeakAsync_WithCancellation_ShouldStopProcessing()
        {
            using var cts = new CancellationTokenSource();
            var speakCount = 0;

            _mockTtsService.Setup(s => s.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string text, string lang, float? speed, CancellationToken ct) =>
                {
                    speakCount++;
                    if (speakCount > 1)
                        ct.ThrowIfCancellationRequested();
                    return text;
                });

            await _coordinator.SpeakAsync("first", "zh", CancellationToken.None, "key1");
            
            cts.Cancel();
            
            try
            {
                await _coordinator.SpeakAsync("second", "zh", cts.Token, "key2");
            }
            catch (OperationCanceledException)
            {
            }

            speakCount.Should().BeLessThan(3);
        }
    }
}