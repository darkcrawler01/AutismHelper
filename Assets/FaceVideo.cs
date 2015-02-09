using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class FaceVideo : MonoBehaviour {
	// Emotion variables
	PXCMSenseManager _pxcmSenseManager;
	
	bool _recordToExternalFile = false;
	private Texture2D _operatorVideoTexture = null;
	
	public RawImage operatorVideoScreen = null;
	
	private string[] EmotionLabels = {"ANGER","CONTEMPT","DISGUST","FEAR","JOY","SADNESS","SURPRISE"};
	private string[] SentimentLabels = {"NEGATIVE","POSITIVE","NEUTRAL"};
	
	public int NUM_EMOTIONS  = 10;
	public int NUM_PRIMARY_EMOTIONS = 7;
	
	public enum Emotions  {ANGER,CONTEMPT,DISGUST,FEAR,JOY,SADNESS,SURPRISE};

	// Voice Recognition
#region VOICE_RECOGNITION
	PXCMAudioSource source;
	PXCMSpeechRecognition sr;
	
	[SerializeField] GameObject uiDropdownButtonPrefab;
	[SerializeField] Transform sourceDropdown;
	[SerializeField] Transform moduleDropdown;
	[SerializeField] Text deviceDropdownText;
	[SerializeField] Text moduleDropdownText;

	[SerializeField] GameObject videoPanel;
	[SerializeField] GameObject sidePanel;
	[SerializeField] GameObject feedbackPanel;
	[SerializeField] GameObject welcomePanel;
	[SerializeField] GameObject answerPanel;
	[SerializeField] GameObject finalPanel;
	
	[SerializeField] Button expectedAnswer;
	[SerializeField] Button userAnswer;

	
	private PXCMSession.ImplDesc desc = null;
	private PXCMAudioSource.DeviceInfo dinfo = null;
	private PXCMSession session = null;

	void OnRecognition(PXCMSpeechRecognition.RecognitionData data)
	{
		if (data.scores[0].label < 0)
		{
			Debug.Log(data.scores[0].sentence);
			if (data.scores[0].tags.Length > 0)
				Debug.Log(data.scores[0].tags);
		}
		else
		{
			for (int i = 0; i < PXCMSpeechRecognition.NBEST_SIZE; i++)
			{
				int label = data.scores[i].label;
				int confidence = data.scores[i].confidence;
				if (label < 0 || confidence == 0) continue;
				Debug.Log(label + "==" + confidence);
			}
			if (data.scores[0].tags.Length > 0)
				Debug.Log(data.scores[0].tags);
		}
		
	}
	
	void OnAlert(PXCMSpeechRecognition.AlertData data)
	{
		Debug.Log(data.label);
	}

	
	public bool SetVocabularyFromFile(String VocFilename)
	{
		pxcmStatus sts = sr.AddVocabToDictation(PXCMSpeechRecognition.VocabFileType.VFT_LIST, VocFilename);
		if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR) return false;
		
		return true;
	}

	
	private void PopulateSource(PXCMSession session)
	{
		if (sourceDropdown == null) 
		{
			Debug.LogError("sourceDropdown property not set");	
			return;
		}
		PXCMAudioSource source = session.CreateAudioSource();
		if (source != null)
		{
			source.ScanDevices();
			
			for (int i = 0; ; i++)
			{
				PXCMAudioSource.DeviceInfo dinfo;
				if (source.QueryDeviceInfo(i, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

				Debug.Log ("Source =" + dinfo.name);
				
				GameObject newButton = (GameObject) Instantiate(uiDropdownButtonPrefab);
				newButton.GetComponentInChildren<Text>().text = dinfo.name;
				newButton.GetComponent<Button>().onClick.AddListener(
					() => {SetSource(dinfo);}
				);
				newButton.transform.SetParent(sourceDropdown);

				deviceDropdownText.text =  dinfo.name;
				this.dinfo = dinfo;

			}
			
			source.Dispose();
		}

	}


	private void PopulateModule(PXCMSession session) {
		if (moduleDropdown == null) 
		{
			Debug.LogError("moduleDropdown property not set");	
			return;
		}
		PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
		desc.cuids[0] = PXCMSpeechRecognition.CUID;
		for (int i = 0; ; i++)
		{
			PXCMSession.ImplDesc desc1;
			if (session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
			Debug.Log ("Module =" + desc1.friendlyName);

			GameObject newButton = (GameObject) Instantiate(uiDropdownButtonPrefab);
			newButton.GetComponentInChildren<Text>().text = desc1.friendlyName;
			newButton.GetComponent<Button>().onClick.AddListener(
				() => {SetModule(desc1);}
			);
			newButton.transform.SetParent(moduleDropdown);
			
			moduleDropdownText.text =  desc1.friendlyName;
			this.desc = desc1;
		}
	}

	private PXCMSpeechRecognition.ProfileInfo pinfo;

	private void PopulateLanguage(PXCMSession session)
	{
		if (desc == null)	return;

		// PXCMSession.ImplDesc desc1 = new PXCMSession.ImplDesc();
		// desc1.cuids [0] = this.desc.iuid;
		//desc.iuid=GetCheckedModule();
		
		PXCMSpeechRecognition vrec;
		if (session.CreateImpl<PXCMSpeechRecognition>(desc, out vrec) < pxcmStatus.PXCM_STATUS_NO_ERROR) return;

		for (int i = 0; ; i++)
		{
			if (vrec.QueryProfile(i,out pinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
			Debug.Log(pinfo.language);
		}
		vrec.Dispose();

	}

	
	private void SetSource(PXCMAudioSource.DeviceInfo dinfo)
	{
		this.dinfo = dinfo;
		this.deviceDropdownText.text = dinfo.name;
	}
	
	private void SetModule(PXCMSession.ImplDesc desc)
	{
		this.desc = desc;
		this.moduleDropdownText.text = desc.friendlyName;
	}


	IEnumerator InitializeAudio() 
	{
		PXCMAudioSource source = session.CreateAudioSource();
		/* Set audio volume to 0.2 */
		source.SetVolume(0.2f);
		
		/* Set Audio Source */
		source.SetDevice(dinfo);
		
		/* Set Module */
		PXCMSession.ImplDesc mdesc = new PXCMSession.ImplDesc();
		mdesc.iuid= desc.iuid;

		pxcmStatus sts = session.CreateImpl<PXCMSpeechRecognition>(out sr);
		if (sts >= pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			/* Configure */
			//PXCMSpeechRecognition.ProfileInfo pinfo;
			//sr.QueryProfile(form.GetCheckedLanguage(), out pinfo);
			sr.SetProfile(this.pinfo);

			PXCMSpeechRecognition.Handler handler = new PXCMSpeechRecognition.Handler();
			handler.onRecognition=OnRecognition;
			handler.onAlert=OnAlert;
			Debug.LogWarning("Starting Rec");
			sts = sr.StartRec(source, handler);
			if (sts>=pxcmStatus.PXCM_STATUS_NO_ERROR) {
				Debug.Log("Init OK");
				yield return new WaitForSeconds (5); 
				sr.StopRec();
			} else {
				Debug.LogError("Failed to initialize");
			}

		}
		source.Dispose ();
		yield break;
	}

	
	#endregion

	private void InitializeRealSense()
	{
	
		_pxcmSenseManager = PXCMSenseManager.CreateInstance();
		if (_pxcmSenseManager == null) 
		{
			Debug.Log("PXCMSenseManager.CreateInstance() failed.");
			return;
		}
		
		pxcmStatus status = _pxcmSenseManager.EnableFace();
		if (status < pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			Debug.Log("PXCMSenseManager.EnableFace() failed.");
			return;
		}
		Debug.Log ("EnableFace initialized");

		/////////// EMOTION
		status = _pxcmSenseManager.EnableEmotion ();
		if (status < pxcmStatus.PXCM_STATUS_NO_ERROR) 
		{
			Debug.Log("PXCMCaptureManager.EnableEmotion() failed with status: " + status + ".");
			
		}
		Debug.Log ("EnableEmotion initialized");

		// Enable hand tracking
		status = _pxcmSenseManager.EnableHand();
		PXCMHandModule hand= _pxcmSenseManager.QueryHand();
		if (status < pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			Debug.Log("PXCMSenseManager.EnableHand() failed.");
			return;
		}
		Debug.Log ("EnableHand initialized");

		status = _pxcmSenseManager.Init ();
		if (status < pxcmStatus.PXCM_STATUS_NO_ERROR) 
		{
			Debug.Log("PXCMCaptureManager.EnableEmotion() Init failed with status: " + status + ".");
			
		}
		Debug.Log ("PxcmSenseManager Initialized");

		if (hand != null) 
		{
			// hcfg is a PXCMHandConfiguration instance
			PXCMHandConfiguration hcfg=hand.CreateActiveConfiguration();

			hcfg.EnableGesture("swipe");
			hcfg.EnableGesture("spreadfingers");

			hcfg.EnableAlert (PXCMHandData.AlertType.ALERT_HAND_NOT_DETECTED);
			hcfg.ApplyChanges ();
/*
			// Load a gesture pack
			hcfg.LoadGesturePack("navigation");
			
			// Enable a gesture
			hcfg.EnableGesture("swipe");
			
			// Apply changes
			hcfg.ApplyChanges();
*/
			hcfg.Dispose ();
			
			Debug.Log ("UpdateHandGestures");
		}

	}

	[SerializeField] Text emotionText;
	
	[SerializeField] GameObject faceDetectedImage;
	[SerializeField] GameObject faceNotDetectedImage;

	private void UpdateEmotion()
	{
		if (currentActivePanel != Panels.Video || movieTexture == null || !movieTexture.isPlaying )		return;


		Debug.Log ("UpdateEmotion");
		PXCMEmotion ft = _pxcmSenseManager.QueryEmotion();
		if (ft == null)
		{
			return;
		}
		
		//GZ DisplayPicture(pp.QueryImageByType(PXCMImage.ImageType.IMAGE_TYPE_COLOR));
		PXCMCapture.Sample sample = _pxcmSenseManager.QueryEmotionSample();
		if (sample == null)
		{
			return;
		}
		
		int numFaces = ft.QueryNumFaces();
		
		if (numFaces == 0)
		{
			faceDetectedImage.SetActive(false);
			faceNotDetectedImage.SetActive(true);
		}
		else
		{
			faceDetectedImage.SetActive(true);
			faceNotDetectedImage.SetActive(false);
		}

		for (int i=0; i<numFaces;i++) {
			/* Retrieve emotionDet location data */
			PXCMEmotion.EmotionData[] arrData = new PXCMEmotion.EmotionData[NUM_EMOTIONS];
			if(ft.QueryAllEmotionData(i, out arrData) >= pxcmStatus.PXCM_STATUS_NO_ERROR){
				bool emotionPresent = false;
				int epidx = -1; int maxscoreE = -3; float maxscoreI = 0;
				for (int j = 0; j < NUM_PRIMARY_EMOTIONS; j++)
				{
					if (arrData[j].evidence  < maxscoreE) continue;
					if (arrData[j].intensity < maxscoreI) continue;
					maxscoreE = arrData[j].evidence;
					maxscoreI = arrData[j].intensity;
					epidx = j;
				}
				if ((epidx != -1) && (maxscoreI > 0.4))
				{
					emotionText.text += (((int)elapsedTime / 60) + ":"+ ((int)elapsedTime % 60) + "===" + EmotionLabels[epidx]) + "\n";

					emotionPresent = true;
				}
				
				int spidx = -1;
				if (emotionPresent)
				{
					maxscoreE = -3; maxscoreI = 0;
					for (int k = 0; k < (NUM_EMOTIONS - NUM_PRIMARY_EMOTIONS); k++)
					{
						if (arrData[NUM_PRIMARY_EMOTIONS + k].evidence  < maxscoreE) continue;
						if (arrData[NUM_PRIMARY_EMOTIONS + k].intensity < maxscoreI) continue;
						maxscoreE = arrData[NUM_PRIMARY_EMOTIONS + k].evidence;
						maxscoreI = arrData[NUM_PRIMARY_EMOTIONS + k].intensity;
						spidx = k;
					}
					if ((spidx != -1))
					{
						Debug.LogWarning( SentimentLabels[spidx]);
					}
				}
				
			}

			return ; // finding only 1 face
		}

	}

	void UpdateHandGestures() 
	{	
		if (currentActivePanel != Panels.Video)	return;

		/* Retrieve hand tracking Module Instance */
		PXCMHandModule handAnalyzer = _pxcmSenseManager.QueryHand();
		
		if (handAnalyzer != null) 
		{
			Debug.Log ("handAnalyzer");
			/* Retrieve hand tracking Data */
			PXCMHandData _handData = handAnalyzer.CreateOutput ();
			if (_handData != null) 
			{
				Debug.Log ("_handData");
				_handData.Update ();
				
				/* Retrieve Gesture Data to manipulate GUIText */
				PXCMHandData.GestureData gestureData;
				for (int i = 0; i < _handData.QueryFiredGesturesNumber(); i++)
				{
					
					Debug.Log ("QueryFiredGesturesNumber");
					if (_handData.QueryFiredGestureData (i, out gestureData) == pxcmStatus.PXCM_STATUS_NO_ERROR)
						DisplayGestures (gestureData);

				}
					
				
					/* Retrieve Alert Data to manipulate GUIText */
				PXCMHandData.AlertData alertData;
				for (int i=0; i<_handData.QueryFiredAlertsNumber(); i++)
					if (_handData.QueryFiredAlertData (i, out alertData) == pxcmStatus.PXCM_STATUS_NO_ERROR)
						ProcessAlerts (alertData);

			}
			_handData.Dispose ();
		}
		
		handAnalyzer.Dispose ();

	}

	private IEnumerator GestureDetected(string gesture)
	{
		Debug.Log (gesture);
		yield break ;
	}

	//Display Gestures
	void DisplayGestures (PXCMHandData.GestureData gestureData)
	{
		if (gestureData.name == "swipe") 
		{
			Next ();	
			StartCoroutine(GestureDetected("swipe"));
		}
		else if (gestureData.name == "spreadfingers")
		{
			PlayPauseVideo();
			StartCoroutine(GestureDetected("spreadfingers"));

		}

		Debug.Log ("left = " + gestureData.name);
				/*
		if (handList.ContainsKey (gestureData.handId)) {
			switch ((PXCMHandData.BodySideType)handList [gestureData.handId]) {
			case PXCMHandData.BodySideType.BODY_SIDE_LEFT: 
				Debug.Log ("left = " + gestureData.name);
				break;
			case PXCMHandData.BodySideType.BODY_SIDE_RIGHT: 
				Debug.Log ("right = " + gestureData.name);
				break;
			}
		}
		*/
	}
	
	//Process Alerts to keep track of hands for Gesture Display
	void ProcessAlerts (PXCMHandData.AlertData alertData)
	{
		Debug.Log ("Left hand not detected == " + alertData.handId);
		/*
		if (handList.ContainsKey (alertData.handId)) {
			switch ((PXCMHandData.BodySideType)handList [alertData.handId]) {
			case PXCMHandData.BodySideType.BODY_SIDE_LEFT: 
				Debug.Log ("Left hand not detected"); 
				break;
			case PXCMHandData.BodySideType.BODY_SIDE_RIGHT:
				Debug.Log ("Right hand not detected");
				break;
			}
		}
		*/
	}

	#region OperatorVideo
	
	private PXCMSizeI32 size=new PXCMSizeI32();
	private bool faceDetected = true;

	private void UpdateColorImage()
	{
		// Retrieve the color image if ready
		PXCMCapture.Sample pxcmSample = _pxcmSenseManager.QueryFaceSample();
		
		PXCMImage pxcmImage = pxcmSample.color;

		if (pxcmImage != null)
		{
			if (_operatorVideoTexture==null) {
				
				/* Save size and preallocate the Texture2D */
				size.width=pxcmImage.info.width;
				size.height=pxcmImage.info.height;
				
				_operatorVideoTexture = new Texture2D((int)size.width, (int)size.height, TextureFormat.ARGB32, false);
				
				/* Associate the texture to the game object */	
				if (operatorVideoScreen)
					operatorVideoScreen.texture = _operatorVideoTexture;						
				
			}  
			PXCMImage.ImageData pxcmImageData;
			pxcmImage.AcquireAccess(PXCMImage.Access.ACCESS_READ,PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out pxcmImageData);
			pxcmImageData.ToTexture2D(0, _operatorVideoTexture);
			pxcmImage.ReleaseAccess(pxcmImageData);
			_operatorVideoTexture.Apply();
		}
	}

	#endregion


	// Use this for initialization
	void Awake () 
	{
		session = PXCMSession.CreateInstance();
		PopulateSource (session);
		PopulateModule (session);
		PopulateLanguage (session);

		SetActivePanel (Panels.Welcome);

		videos = new VideoDetails[2];
		videos [0] = new VideoDetails ("small2", Emotions.ANGER);
		videos [1] = new VideoDetails ("small2", Emotions.ANGER);
	}

	void Start() 
	{

	}
	
	//	Update is called once per frame

	public void Update()
	{
		if (_pxcmSenseManager == null)
		{
			Debug.Log ("_pxcmSenseManager is null");
			return;
		}
		pxcmStatus status = _pxcmSenseManager.AcquireFrame (true);
		// Wait until any frame data is available
		if (status < pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			Debug.Log ("_pxcmSenseManager.AcquireFrame failed");
			return;
		}
		
		UpdateColorImage();

		if (currentActivePanel == Panels.Video) 
		{
			UpdateHandGestures ();

			UpdateEmotion ();
		}

		// Process the next frame
		_pxcmSenseManager.ReleaseFrame();
	}


	#region VideoPanel
	[SerializeField] RawImage videoRawImage;
	
	// Face Video images
	private AudioSource a;
	private MovieTexture movieTexture;
	private bool isPaused = false;
	private VideoDetails[] videos;
	private int videoID = -1;
	private float elapsedTime, startTime;
	
	// Utility/Helper functions
	private IEnumerator LoadVideo()
	{
		if (videoRawImage == null)
		{
			Debug.LogError("operatorRawImage is not set");
			yield break;
		}
		if (a == null)	a = gameObject.AddComponent<AudioSource> ();

		videoID++;

		movieTexture =  Resources.Load<MovieTexture> (videos [videoID].VideoName);
		if (movieTexture == null)	Debug.LogError ("small is null");

		// Wait till it's ready to start playing
		while (!movieTexture.isReadyToPlay)	yield return null;
		
		// Assign the movie texture to our texture view
		videoRawImage.texture = movieTexture;

		// Set the audio source
		a.clip = movieTexture.audioClip;
		
		// Finally play the video
		movieTexture.Play();
		startTime = Time.time;

		a.Play();
		isPaused = false;

		while (isPaused || movieTexture.isPlaying) 
		{
			elapsedTime = Time.time;
			elapsedTime = elapsedTime - startTime;
			yield return null;
		}	
		yield return new WaitForSeconds(1.0f);

		GoToFeedbackPanel ();

	}
	
	public void PlayPauseVideo() 
	{
		if (videoID == -1)
		{
			StartCoroutine(LoadVideo());
		}
		else if (movieTexture != null && movieTexture.isPlaying) 
		{
			movieTexture.Pause();
			a.Pause();
			isPaused = true;
		} 
		else if (movieTexture != null && isPaused) 
		{
			movieTexture.Play();
			a.Play();
		}
		else 
		{
			// video finished
			Debug.LogError ("video finished");
			StartCoroutine(LoadVideo());
		}
		Debug.LogError ("button pressed");
	}
	
	public void Next() 
	{
		if (movieTexture != null)
		{
			if (movieTexture.isPlaying)
			{
				a.Stop();
				movieTexture.Stop();
			} 
			else if (isPaused)
			{
				movieTexture.Stop();
				a.Stop();
				isPaused = false;
			}
			
		}
	}

	class VideoDetails
	{
		public string VideoName { get; set;}
		public Emotions ExpectedEmotion { get; set; }
		
		public VideoDetails(string videoName, Emotions expectedEmotion)
		{
			this.VideoName = videoName;
			this.ExpectedEmotion = expectedEmotion;
		}
	}
	#endregion

	#region Navigation
	
	[SerializeField] Text answerText;
	private Emotions selectedEmotion;
	
	private enum Panels
	{
		Welcome,
		Feedback,
		Video,
		Answer,
		Final
	};
	private Panels currentActivePanel;

	private void SetActivePanel(Panels panel)
	{
		currentActivePanel = panel;
		welcomePanel.SetActive (false);
		feedbackPanel.SetActive (false);
		videoPanel.SetActive (false);
		answerPanel.SetActive (false);
		finalPanel.SetActive (false);

		if (panel == Panels.Answer)
		{
			answerPanel.SetActive(true);
		} 
		else if (panel == Panels.Welcome) 
		{
			welcomePanel.SetActive(true);		
		}
		else if (panel == Panels.Feedback) 
		{
			feedbackPanel.SetActive(true);
		}
		else if (panel == Panels.Video) 
		{
			videoPanel.SetActive(true);
		}
		else if (panel == Panels.Final) 
		{
			finalPanel.SetActive(true);
		}
	}

	private void GoToFeedbackPanel()
	{
		SetActivePanel (Panels.Feedback);
		StartCoroutine( InitializeAudio ());
	}
	public void StartButtonAction()
	{
		SetActivePanel (Panels.Video);
		InitializeRealSense ();
	}

	public void FeedbackButtonAction(string emotion)
	{
		selectedEmotion = (Emotions)Enum.Parse (typeof(Emotions), emotion);
		SetActivePanel (Panels.Answer);
		userAnswer.GetComponentInChildren<Text>().text = selectedEmotion.ToString();
		expectedAnswer.GetComponentInChildren<Text> ().text = videos [videoID].ExpectedEmotion.ToString();
		var colors = userAnswer.colors;

		if (selectedEmotion == videos [videoID].ExpectedEmotion)
		{
			answerText.text = "Correct !";
			colors.disabledColor = Color.green;
		}
		else
		{
			answerText.text = "InCorrect !";
			colors.disabledColor = Color.red;
		}
		userAnswer.colors = colors;
	}

	public void ContinueButtonAction()
	{
		Debug.Log (videoID);
		if (videoID >= videos.Length - 1)
		{
			SetActivePanel(Panels.Final);
			return;
		}
//		Image img = Resources.Load<Image> ("blank_video_screen");

		//videoRawImage.texture = img.mainTexture;
		SetActivePanel(Panels.Video);
	}
	#endregion
}

