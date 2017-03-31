using System;
using System.Net;
using System.Net.Http;
using Windows.UI.Xaml.Controls;
using Windows.UI.Core;
using Windows.Media.SpeechRecognition;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Media.SpeechSynthesis;
using Newtonsoft.Json;

// TODO List: 
// Handle recognizer initialization failure
// Wrap everything in try/catch
// handle GPIO init failure
// handle Synth failure
// handle recognition failure

namespace SpeechInterface
{
    public sealed partial class MainPage
    {
        #region Globals
        private CoreDispatcher _dispatcher;
        private SpeechRecognizer _speechRecognizer;
        private bool _isListening;
        private SpeechSynthesizer _synthesizer = new SpeechSynthesizer();
        private IAsyncOperation<SpeechRecognitionResult> recognitionOperation;
        private ResourceContext _speechContext;
        private ResourceMap _speechResourceMap;
        private const int BUTTON_PIN = 26;
        private GpioPin buttonPin;
        private GpioPin resetPin;
        private MediaElement mediaElement = new MediaElement();
        private bool StartConvo = true;
        private string convoId;
        private int retry = 0;
        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            _isListening = false;
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            InitializeRecognizer();
        }

        private async void InitializeRecognizer()
        {
            if (_speechRecognizer != null)
            {
                _speechRecognizer.Dispose();
                _speechRecognizer = null;
            }

            _speechRecognizer = new SpeechRecognizer();

            var speechConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.WebSearch, "webSearch");
            _speechRecognizer.Constraints.Add(speechConstraint);

            SpeechRecognitionCompilationResult result = await _speechRecognizer.CompileConstraintsAsync();

            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                //TODO: handle initialization failure
            }
            InitGPIO();
        }

        public async void Recognize()
        {
            if (_isListening == false && _speechRecognizer.State == SpeechRecognizerState.Idle)
            {
                try
                {
                    StatusBlock.Text = "Listening...";
                    _isListening = true;
                    _speechRecognizer.UIOptions.IsReadBackEnabled = false;
                    recognitionOperation = _speechRecognizer.RecognizeWithUIAsync();
                    SpeechRecognitionResult speechRecognitionResult = await recognitionOperation;
                    _isListening = false;

                    if (speechRecognitionResult.Status == SpeechRecognitionResultStatus.Success)
                    {
                        ResultGenerated(speechRecognitionResult);
                    }
                    else
                    {
                        //TODO: handle failure;
                    }

                }
                catch (Exception ex)
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                    await messageDialog.ShowAsync();

                    _isListening = false;
                }
            }
            else
                _isListening = false;
        }

        private async void ResultGenerated(SpeechRecognitionResult args)
        {
            try
            {
                if (args.Confidence == SpeechRecognitionConfidence.Medium ||
                    args.Confidence == SpeechRecognitionConfidence.High)
                {
                    await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        StatusBlock.Text = "Result Generated";
                        DictationTextBox.Text = "Heard you say:\n'" + args.Text + "'";
                    });

                    var botComm = new DirectLineCommunicator();

                    if (StartConvo || convoId == null)
                    {
                        convoId = StartNewConversation(botComm);
                        StartConvo = false;
                    }
                    if (convoId == null)
                    {
                        SpeakText("Sorry, I'm having difficulty initiating connection to the bot.");
                        return;
                    }
                    bool? activitySent = SendActivityToBot(botComm, convoId, args.Text);
                    var responseText = "";
                    switch (activitySent)
                    {
                        case false:
                            SpeakText("Sorry, I'm having difficulty speaking to the bot.");
                            return;
                        case true:
                            responseText = GetActivitiesFromBot(botComm, convoId);
                            break;
                        case null:
                        default:
                            return;
                    }
                    SpeakText(responseText ?? "hmmm... I'm not getting any response from the bot.");
                }
                else
                {
                    SpeakText("I'm not sure I got that. Would you mind asking again?");
                }
            }
            catch (Exception e)
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    StatusBlock.Text = string.Format("Exception Thrown: {0}", e.Message);
                    DictationTextBox.Text = string.Format("inner exception: {0}", e.InnerException);
                });
                throw;
            }
        }

        private string GetActivitiesFromBot(DirectLineCommunicator botComm, string convoId)
        {
            var i = 0;
            while (i++ < 10)
            {
                HttpResponseMessage response = botComm.GetActivities(convoId);
                string errorText = null;
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        break;
                    case HttpStatusCode.Forbidden:
                        errorText = "error: invalid secret or token";
                        break;
                    case HttpStatusCode.Unauthorized:
                        errorText = "error: invalid authorization header";
                        break;
                    case HttpStatusCode.NotFound:
                        errorText = "error: object not found";
                        break;
                    case HttpStatusCode.InternalServerError:
                        errorText = "error: internal server error";
                        break;
                    case HttpStatusCode.BadGateway:
                        errorText = "error: bot unavailable or returned an error";
                        break;
                    case HttpStatusCode.BadRequest:
                        errorText = "error: Bad Request";
                        break;
                    default:
                        errorText = string.Format("error: response code {0}", response.StatusCode);
                        break;
                }
                if (errorText != null)
                {
                    // presently, just returns that there was an error. Any more detail is not really 
                    // necessary in speech interface, but the switch statement above really helps for debugging
                    return "error";
                }

                object responseActivity =
                    JsonConvert.DeserializeObject<GetActivity>(response.Content.ReadAsStringAsync().Result);

                if (((GetActivity)responseActivity).activities[((GetActivity)responseActivity).activities.Length - 1].id != convoId)
                    return ((GetActivity)responseActivity).activities[((GetActivity)responseActivity).activities.Length - 1].text;
                Task.Delay(500);
            }
            return null;
        }

        private bool? SendActivityToBot(DirectLineCommunicator botComm, string convoId, string text)
        {
            HttpResponseMessage sentActivity = botComm.SendActivity(text, convoId);

            string errorText = null;
            switch (sentActivity.StatusCode)
            {
                case HttpStatusCode.NoContent:
                case HttpStatusCode.OK:
                    break;
                case HttpStatusCode.Forbidden:
                    errorText = "error: invalid secret or token";
                    break;
                case HttpStatusCode.Unauthorized:
                    if (retry > 2)
                    {
                        errorText = "error: invalid authorization header";
                        retry = 0;
                    }
                    else
                    {
                        //In the event post comes in unauthorized, attempt to 
                        //create a new convo and try again (max retry 2)
                        this.convoId = StartNewConversation(botComm);
                        SendActivityToBot(botComm, this.convoId, text);
                        return null;
                    }
                    break;
                case HttpStatusCode.NotFound:
                    errorText = "error: object not found";
                    break;
                case HttpStatusCode.InternalServerError:
                    errorText = "error: internal server error";
                    break;
                case HttpStatusCode.BadGateway:
                    errorText = "error: bot unavailable or returned an error";
                    break;
                case HttpStatusCode.BadRequest:
                    errorText = "error: Bad Request";
                    break;
                default:
                    errorText = string.Format("error: response code {0}", sentActivity.StatusCode);
                    break;
            }
            // presently, just returns that there was an error. Any more detail is not really 
            // necessary in speech interface, but the switch statement above really helps for debugging
            return errorText == null;
        }

        private string StartNewConversation(DirectLineCommunicator botComm)
        {
            HttpResponseMessage newConversation = botComm.StartConversation();
            object responseObject =
            JsonConvert.DeserializeObject<StartConvo>(newConversation.Content.ReadAsStringAsync().Result);

            string convoId = ((StartConvo)responseObject).conversationId;
            HttpStatusCode responseCode = newConversation.StatusCode;

            string errorText = null;

            switch (responseCode)
            {
                case HttpStatusCode.Created:
                case HttpStatusCode.OK:
                    break;
                case HttpStatusCode.Forbidden:
                    errorText = "error: invalid secret or token";
                    break;
                case HttpStatusCode.Unauthorized:
                    errorText = "error: invalid authorization header";
                    break;
                case HttpStatusCode.Conflict:
                    errorText = "error: object already exists";
                    break;
                case HttpStatusCode.NotFound:
                    errorText = "error: Not Found";
                    break;
                default:
                    errorText = string.Format("error: unexpected response code {0}", responseCode);
                    break;
            }
            // presently, just returns that there was an error. Any more detail is not really 
            // necessary in speech interface, but the switch statement above really helps for debugging
            return errorText == null ? convoId : null;

        }

        public async void SpeakText(string text)
        {
            try
            {
                foreach (VoiceInformation voice in SpeechSynthesizer.AllVoices)
                {
                    if (voice.DisplayName == "Microsoft Zira Mobile")
                    {
                        _synthesizer.Voice = voice;
                    }
                }

                SpeechSynthesisStream stream = await _synthesizer.SynthesizeTextToStreamAsync(text);

                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    mediaElement.SetSource(stream, stream.ContentType);
                    mediaElement.Play();
                    mediaElement.Stop();
                });
            }
            catch (Exception e)
            {
                //TODO: error handling for speech sythesis
            }
        }

        private void InitGPIO()
        {
            GpioController gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                //TODO: handle GPIO INIT Failure
                return;
            }
            buttonPin = gpio.OpenPin(BUTTON_PIN);
            buttonPin.SetDriveMode(buttonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                ? GpioPinDriveMode.InputPullUp
                : GpioPinDriveMode.Input);

            buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            buttonPin.ValueChanged += buttonPin_ValueChanged;

            StatusBlock.Text = "READY";
        }

        private void buttonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                Recognize();
            }
        }
    }
}