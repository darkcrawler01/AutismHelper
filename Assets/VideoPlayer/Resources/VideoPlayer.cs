using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VideoPlayer : MonoBehaviour {

    // Public configuration
    public bool loadVideoOnStart = true;
    public string movieURL = "";

    // Links to local objects we'll need to interact with
    private RectTransform r;
    private RawImage i;
    private AudioSource a;

    void Awake()
    {	
		r = gameObject.GetComponent<RectTransform> ();
		i = gameObject.AddComponent<RawImage> ();
		a = gameObject.AddComponent<AudioSource> ();

		if (r == null)
			Debug.LogError ("r is null");
		if (i == null)
			Debug.LogError ("i is null");
		if (a == null)
			Debug.LogError ("a is null");
		//HideVideo();
    }

    void Start()
    {
        // If we should call to load on start do so.
        if (loadVideoOnStart)
        {
            StartCoroutine(LoadVideo());
        }
    }

	void Update () {
	
	}

    // Utility/Helper functions
    public IEnumerator LoadVideo()
    {
        //WWW movieLoader = new WWW(movieURL);
		MovieTexture movieTexture =  Resources.Load<MovieTexture> ("small2");
		if (movieTexture == null)
						Debug.LogError ("small is null");
        //MovieTexture movieTexture = movieLoader.movie;

        // Set the size to match the video
        SetViewSize(movieTexture.width, movieTexture.height);

        // Wait till it's ready to start playing
        while (!movieTexture.isReadyToPlay)
            yield return null;

        // Assign the movie texture to our texture view
        i.texture = movieTexture;

        SetViewSize(movieTexture.width, movieTexture.height);

        // Set the audio source
        a.clip = movieTexture.audioClip;

        // Finally play the video
        movieTexture.Play();
        a.Play();
	
        //ShowVideo();
    }

    public void CloseVideo(bool error = false)
    {
        this.enabled = false;
    }

    public void HideVideo()
    {
        Color c = i.color;
        c.a = 0.0f;
        i.color = c;
    }

    public void ShowVideo()
    {
        Color c = i.color;
        c.a = 1.0f;
        i.color = c;
    }

    public void SetViewSize(float width, float height)
    {
        r.sizeDelta = new Vector2(width, height);
    }
}
