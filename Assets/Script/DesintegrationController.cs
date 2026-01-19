using UnityEngine;

public class DesintegrationController : MonoBehaviour
{
    [SerializeField] private float duration = 2f; 
    [SerializeField] private float delay = 0f; 
    [SerializeField] private bool playOnStart = true; 
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Destruction")]
    [SerializeField] private bool destroyAfterDisintegration = true;

    private Material[] materials;
    private float currentTime = 0f;
    private bool isDisintegrating = false;
    private bool hasStarted = false;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            materials = renderer.materials;

 
            foreach (Material mat in materials)
            {
                if (mat.HasProperty("_Weight"))
                {
                    mat.SetFloat("_Weight", 0f);
                }
            }
        }
        else
        {
            Debug.LogError("DisintegrationController: Aucun Renderer trouvé sur " + gameObject.name);
        }

        if (playOnStart)
        {
            Invoke(nameof(StartDisintegration), delay);
        }
    }


    void Update()
    {
        if (isDisintegrating && materials != null)
        {
            currentTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentTime / duration);

            float weight = curve.Evaluate(progress);

            foreach (Material mat in materials)
            {
                if (mat != null && mat.HasProperty("_Weight"))
                {
                    mat.SetFloat("_Weight", weight);
                }
            }

            if (progress >= 1f)
            {
                isDisintegrating = false;
                OnDisintegrationComplete();
            }
        }
    }
    public void StartDisintegration()
    {
        if (!hasStarted)
        {
            hasStarted = true;
            isDisintegrating = true;
            currentTime = 0f;
        }
    }

    public void Reset()
    {
        if (materials != null)
        {
            foreach (Material mat in materials)
            {
                if (mat != null && mat.HasProperty("_Weight"))
                {
                    mat.SetFloat("_Weight", 0f);
                }
            }
        }
        currentTime = 0f;
        isDisintegrating = false;
        hasStarted = false;
    }

    private void OnDisintegrationComplete()
    {
        if (destroyAfterDisintegration)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (materials != null)
        {
            foreach (Material mat in materials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }

    [ContextMenu("Tester Désintégration")]
    public void TestDisintegration()
    {
        Reset();
        StartDisintegration();
    }
}
