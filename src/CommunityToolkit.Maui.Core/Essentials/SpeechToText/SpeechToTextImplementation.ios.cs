using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AVFoundation;
using Speech;

namespace CommunityToolkit.Maui.Media;

/// <inheritdoc />
public sealed partial class SpeechToTextImplementation
{
	[MemberNotNull(nameof(audioEngine), nameof(recognitionTask), nameof(liveSpeechRequest))]
	Task InternalStartListeningAsync(CultureInfo culture, bool isOnline, CancellationToken cancellationToken)
	{
		speechRecognizer = new SFSpeechRecognizer(NSLocale.FromLocaleIdentifier(culture.Name));

		if (isOnline && UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			speechRecognizer.SupportsOnDeviceRecognition = true;
		}

		if (!speechRecognizer.Available)
		{
			throw new ArgumentException("Speech recognizer is not available");
		}

		audioEngine = new AVAudioEngine();
		liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest()
		{
			ShouldReportPartialResults = true
		};
		if (isOnline && UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			liveSpeechRequest.RequiresOnDeviceRecognition = true;
		}

		InitializeAvAudioSession(out _);

		var node = audioEngine.InputNode;
		var recordingFormat = node.GetBusOutputFormat(0);
		node.InstallTapOnBus(0, 1024, recordingFormat, (buffer, _) => liveSpeechRequest.Append(buffer));

		audioEngine.Prepare();
		audioEngine.StartAndReturnError(out var error);

		if (error is not null)
		{
			throw new ArgumentException("Error starting audio engine - " + error.LocalizedDescription);
		}

		cancellationToken.ThrowIfCancellationRequested();

		var currentIndex = 0;
		recognitionTask = speechRecognizer.GetRecognitionTask(liveSpeechRequest, (result, err) =>
		{
			if (err is not null)
			{
				StopRecording();
				OnRecognitionResultCompleted(SpeechToTextResult.Failed(new Exception(err.LocalizedDescription)));
			}
			else
			{
				if (result.Final)
				{
					currentIndex = 0;
					StopRecording();
					OnRecognitionResultCompleted(SpeechToTextResult.Success(result.BestTranscription.FormattedString));
				}
				else
				{
					if (currentIndex <= 0)
					{
						OnSpeechToTextStateChanged(CurrentState);
					}

					for (var i = currentIndex; i < result.BestTranscription.Segments.Length; i++)
					{
						var s = result.BestTranscription.Segments[i].Substring;
						currentIndex++;
						OnRecognitionResultUpdated(s);
					}
				}
			}
		});

		return Task.CompletedTask;
	}

	[MemberNotNull(nameof(audioEngine), nameof(recognitionTask), nameof(liveSpeechRequest))]
	Task InternalStartListeningAsync(CultureInfo culture, CancellationToken cancellationToken)
	{
		return InternalStartListeningAsync(culture, true, cancellationToken);
	}

	[MemberNotNull(nameof(audioEngine), nameof(recognitionTask), nameof(liveSpeechRequest))]
	Task InternalStartOfflineListeningAsync(CultureInfo culture, CancellationToken cancellationToken)
	{
		if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			throw new NotSupportedException("Offline listening is supported on iOS 13 and later");
		}

		return InternalStartListeningAsync(culture, false, cancellationToken);
	}
}