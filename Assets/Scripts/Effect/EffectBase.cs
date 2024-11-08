using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using Xiaobo.UnityToolkit.Core;

public class EffectBase : MonoBehaviour
{
    [SerializeField]
    protected VisualEffect vfx = null;
    [SerializeField]
    protected Material mat = null;

    protected Player player;

    protected bool needPushHumanStencil = false;
    DepthImageProcessor depthImageProcessor;
    protected bool needPushBuffer = false;
    MeshToBufferConvertor meshToBufferConverter;

    protected int effectIndex = -1;

    protected bool isOn = false;
    protected float effectAlpha = 0;

    public Color effectColor = Color.white;

    protected Vector2 effectRange = Vector2.zero;    
    public float effectMaxRange = 30;
    public float effectWidth = 2;

    protected float marchingDistance = 0;
    public Remap.MapMode marchingMode = Remap.MapMode.Wrap;
    public float marchingSpeed = 1;

    void Awake()
    {
        depthImageProcessor = FindFirstObjectByType<DepthImageProcessor>();
        if(depthImageProcessor == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find DepthImageProcessor");
        }

        meshToBufferConverter = FindFirstObjectByType<MeshToBufferConvertor>();
        if (meshToBufferConverter == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find MeshToBufferConverter");
        }

        effectIndex = transform.GetSiblingIndex();
    }

    public virtual void StartEffect()
    {
        player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();

        isOn = true;

        marchingDistance = 0;
        effectRange = Vector2.zero;
    }

    public virtual void StopEffect()
    {
        isOn = false;

        marchingDistance = 0;
        effectRange = Vector2.zero;
    }

    

    public virtual void UpdateVFXParameter()
    {
        if (isOn == false || GameManager.Instance.GameMode == GameMode.Undefined)
            return;

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null || NetworkManager.Singleton.LocalClient.PlayerObject.IsSpawned == false)
            return;

        if (player == null || player.currentEffectIndex.Value != effectIndex && player.targetEffectIndex.Value != effectIndex)
            return;


        // Update blending alpha
        effectAlpha = player.currentEffectIndex.Value == effectIndex ? 1 - player.effectLerp.Value : player.effectLerp.Value;


        // Update marching range
        marchingDistance += Time.deltaTime * marchingSpeed;
        effectRange.x = Remap.Map(marchingDistance, 0, effectMaxRange, 0, effectMaxRange, marchingMode);
        effectRange.y = Remap.Map(Mathf.Max(0, marchingDistance - effectWidth), 0, effectMaxRange, 0, effectMaxRange, marchingMode);


        // Update VFX
        PushParametersToVFX();


        // Update meshing material
        PushParametersToMaterial();
        
    }

    void PushParametersToVFX()
    {
        if (vfx != null)
        {
            vfx.SetBool("IsOn", isOn);
            vfx.SetFloat("Alpha", effectAlpha);
            vfx.SetVector4("EffectColor", effectColor);
            vfx.SetVector2("EffectRange", effectRange);
            vfx.SetFloat("EffectWidth", effectWidth);
            vfx.SetVector3("Player_position", player.Body.position);
            vfx.SetVector3("Player_angles", player.Body.eulerAngles);

            if (needPushHumanStencil == true && depthImageProcessor != null && depthImageProcessor.HumanStencilTexture != null)
            {
                Texture2D human_tex = depthImageProcessor.HumanStencilTexture;
                human_tex.wrapMode = TextureWrapMode.Repeat;
                if (vfx.HasTexture("HumanStencilTexture")) vfx.SetTexture("HumanStencilTexture", human_tex);
                if (vfx.HasMatrix4x4("HumanStencilTextureMatrix")) vfx.SetMatrix4x4("HumanStencilTextureMatrix", depthImageProcessor.DisplayRotatioMatrix);
            }

            if (needPushBuffer == true && meshToBufferConverter != null)
            {
                vfx.SetInt("VertexCount", meshToBufferConverter.VertexCount);
                if (meshToBufferConverter.VertexBuffer != null && vfx.HasGraphicsBuffer("VertexBuffer"))
                {
                    vfx.SetGraphicsBuffer("VertexBuffer", meshToBufferConverter.VertexBuffer);
                }
                if (meshToBufferConverter.NormalBuffer != null && vfx.HasGraphicsBuffer("NormalBuffer"))
                {
                    vfx.SetGraphicsBuffer("NormalBuffer", meshToBufferConverter.NormalBuffer);
                }
            }
        }
    }

    void PushParametersToMaterial()
    {
        if (mat != null)
        {
            mat.SetInt("_IsOn", isOn ? 1 : 0);
            mat.SetFloat("_Alpha", effectAlpha);
        }
    }

    void Update()
    {
        UpdateVFXParameter();
    }
}
