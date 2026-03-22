using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResXRSceneManager : ResXRSingleton<ResXRSceneManager>
{
    // declare by name each scene and make it public so it can be accessed easily from other scripts.
    [Header("Base Scene Index")]
    [SerializeField]
    public int BaseSceneIndex = 0;

    [Header("Build index of the first scene to load")]
    [SerializeField]
    public int FirstSceneToLoadIndex = 1;

    private float FADETOBLACKDURATION = 2.5f;
    private float FADETOCLEARDURATION = 1.5f;

    private string _baseSceneName;
    private string currentSceneName;
    public string CurrentSceneName => currentSceneName;

    private bool _shouldRepositionPlayer;

    // gets isProjectUsingCalibration to know whether to use PlayerRepositioner or not.
    public void Init(bool isProjectUsingCalibration)
    {
        // if project is being calibrated to a space then moving it to a PlayerRepositioner will ruin calibration.
        _shouldRepositionPlayer = !isProjectUsingCalibration;
        _baseSceneName = SceneManager.GetSceneByBuildIndex(BaseSceneIndex).name;
        // the script assumes that Base Scene is always the first in the build order.
        if (Application.isEditor)
        {
            InitializeInEditor();
        }
        else
        {
            InitializeInBuild();
        }
    }

    private void InitializeInEditor()
    {
        // do nothing if scene count is not bigger than 1
        if (SceneManager.sceneCount < 2)
        {
            Debug.LogWarning("Scene count is less than 2, no scene initialization");
            return;
        }

        RepositionPlayerIfNeeded();

        // we assume that when played in editor we'll be in the right scene combination.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            // Get the scene at the specified index
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.name != _baseSceneName)
            {
                currentSceneName = scene.name;
                return;
            }
        }

        // if we got here it means we only have the base scene (for 1 scene projects) and it should be the current
        currentSceneName = _baseSceneName;
    }

    private void InitializeInBuild()
    {
        ResXRPlayer.Instance.FadeViewToColor(Color.black, 0).Forget();
        currentSceneName = _baseSceneName;
        // make sure to launch your starting scene here
        LoadActiveScene(FirstSceneToLoadIndex).Forget();
    }

    async private UniTask LoadActiveScene(int sceneBuildIndex)
    {
        if (SceneManager.GetActiveScene().buildIndex == sceneBuildIndex)
        {
            Debug.LogWarning($"Tried to load scene index {sceneBuildIndex} but it's already the active scene.");
            return;
        }

        await SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(sceneBuildIndex));

        currentSceneName = SceneManager.GetSceneByBuildIndex(sceneBuildIndex).name;
        // reposition player accordingly to new scene
        RepositionPlayerIfNeeded();

        await ResXRPlayer.Instance.FadeViewToColor(Color.clear, FADETOCLEARDURATION);
    }

    async private UniTask LoadActiveScene(string sceneName)
    {
        if (currentSceneName == sceneName)
        {
            Debug.LogWarning($"Tried to load {sceneName} scene but its already loaded");
            return;
        }

        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        currentSceneName = sceneName;

        // reposition player accordingly to new scene
        RepositionPlayerIfNeeded();

        //await ResXRPlayer.Instance.FadeViewToColor(Color.clear, FADETOCLEARDURATION);
    }

    async public UniTask SwitchActiveScene(string sceneName)
    {
        if (currentSceneName == sceneName)
        {
            Debug.LogWarning($"Tried to load {sceneName} scene but its already loaded");
            return;
        }

        await UnloadActiveScene();

        await LoadActiveScene(sceneName);
    }

    async private UniTask UnloadActiveScene()
    {
        await ResXRPlayer.Instance.FadeViewToColor(Color.black, FADETOBLACKDURATION);

        await SceneManager.UnloadSceneAsync(currentSceneName);
    }


    private void RepositionPlayerIfNeeded()
    {
        // if project is being calibrated to a space then moving it to a PlayerRepositioner will ruin calibration.
        if (!_shouldRepositionPlayer) return;

        PlayerRepositioner repositioner = FindFirstObjectByType<PlayerRepositioner>();
        if (repositioner == null) return;
        ResXRPlayer.Instance.RepositionPlayer(repositioner);
    }

    public async void RestartActiveScene()
    {
        await SceneManager.UnloadSceneAsync(FirstSceneToLoadIndex);
        SceneManager.LoadScene(FirstSceneToLoadIndex, LoadSceneMode.Additive);
    }
}