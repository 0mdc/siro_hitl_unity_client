using System.Collections;
using UnityEngine;

/// <summary>
/// Effect that fades the scene to black, using fog, when a scene is being loaded.
/// Works both in traditional interfaces and VR.
/// </summary>
public class LoadingEffectHandler : IKeyframeMessageConsumer
{
    private LoadProgressTracker _loadProgressTracker;
    Coroutine _sceneChangeCoroutine = null;
    CoroutineContainer _coroutines;
    bool _changingScene = false;
    bool _loading = false;

    public LoadingEffectHandler()
    {
        _coroutines = CoroutineContainer.Create("LoadingEffectHandler");

        // Initialize fog
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fog = false;
        RenderSettings.fogDensity = 0.0f;

        _loadProgressTracker = LoadProgressTracker.Instance;
        _loadProgressTracker.OnLoadStarted += LoadStarted;
        _loadProgressTracker.OnLoadFinished += LoadFinished;
    }

    public void Update()
    {
    }

    public void OnSceneChangeBegin()
    {
        RenderSettings.fog = true;
        RenderSettings.fogDensity = 1.0f;

        if (_sceneChangeCoroutine != null)
        {
            _coroutines.StopCoroutine(_sceneChangeCoroutine);
        }
    }

    public void OnSceneChangeEnd()
    {
        _sceneChangeCoroutine = _coroutines.StartCoroutine(ProgressivelyRemoveFog(0.75f));
    }

    IEnumerator ProgressivelyRemoveFog(float duration)
    {
        float initialFogDensity = RenderSettings.fogDensity;
        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            float t = EaseInCubic(elapsedTime / duration);
            float fogDensity = Mathf.Lerp(initialFogDensity, 0.0f, t);
            RenderSettings.fogDensity = fogDensity;

            elapsedTime += Time.deltaTime;
            yield return null; // Skip one frame
        }

        RenderSettings.fog = false;
    }

    public void ProcessMessage(Message message)
    {
        if (message.sceneChanged)
        {
            _changingScene = true;
        }
    }

    void LoadStarted()
    {
        _loading = true;
        if (_changingScene)
        {
            OnSceneChangeBegin();
        }
    }

    void LoadFinished()
    {
        _loading = false;
        _changingScene = false;
        OnSceneChangeEnd();
    }

    static float EaseInCubic(float x) {
        return x * x * x;
    }
}
