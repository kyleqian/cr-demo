﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.iOS;

[RequireComponent(typeof(Camera))]
public class ARKitCameraTexture : MonoBehaviour
{
    public Material m_ClearMaterial;

    CommandBuffer m_VideoCommandBuffer;
    Texture2D _videoTextureY;
    Texture2D _videoTextureCbCr;
    Matrix4x4 _displayTransform;

    bool bCommandBufferInitialized;

    void UpdateFrame(UnityARCamera cam)
    {
        _displayTransform = new Matrix4x4();
        _displayTransform.SetColumn(0, cam.displayTransform.column0);
        _displayTransform.SetColumn(1, cam.displayTransform.column1);
        _displayTransform.SetColumn(2, cam.displayTransform.column2);
        _displayTransform.SetColumn(3, cam.displayTransform.column3);
    }

    void InitializeCommandBuffer()
    {
        m_VideoCommandBuffer = new CommandBuffer();
        m_VideoCommandBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, m_ClearMaterial);
        GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
        bCommandBufferInitialized = true;
    }

    void Start()
    {
        UnityARSessionNativeInterface.ARFrameUpdatedEvent += UpdateFrame;
    }

    void OnDestroy()
    {
        if (m_VideoCommandBuffer != null)
        {
            GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
        }
        UnityARSessionNativeInterface.ARFrameUpdatedEvent -= UpdateFrame;
        bCommandBufferInitialized = false;
    }

#if !UNITY_EDITOR && UNITY_IOS
    void OnPreRender()
    {
        ARTextureHandles handles = ARKitManager.Instance.Session.GetARVideoTextureHandles();
        if (handles.IsNull())
        {
            return;
        }

        if (!bCommandBufferInitialized) {
            InitializeCommandBuffer();
        }

        Resolution currentResolution = Screen.currentResolution;

        // Texture Y
        if (_videoTextureY == null) {
          _videoTextureY = Texture2D.CreateExternalTexture(currentResolution.width, currentResolution.height,
              TextureFormat.R8, false, false, (System.IntPtr)handles.TextureY);
          _videoTextureY.filterMode = FilterMode.Bilinear;
          _videoTextureY.wrapMode = TextureWrapMode.Repeat;
          m_ClearMaterial.SetTexture("_textureY", _videoTextureY);
        }

        // Texture CbCr
        if (_videoTextureCbCr == null) {
          _videoTextureCbCr = Texture2D.CreateExternalTexture(currentResolution.width, currentResolution.height,
              TextureFormat.RG16, false, false, (System.IntPtr)handles.TextureCbCr);
          _videoTextureCbCr.filterMode = FilterMode.Bilinear;
          _videoTextureCbCr.wrapMode = TextureWrapMode.Repeat;
          m_ClearMaterial.SetTexture("_textureCbCr", _videoTextureCbCr);
        }

        _videoTextureY.UpdateExternalTexture(handles.TextureY);
        _videoTextureCbCr.UpdateExternalTexture(handles.TextureCbCr);

        m_ClearMaterial.SetMatrix("_DisplayTransform", _displayTransform);
    }
#else
    public void SetYTexure(Texture2D YTex)
    {
        _videoTextureY = YTex;
    }

    public void SetUVTexure(Texture2D UVTex)
    {
        _videoTextureCbCr = UVTex;
    }

    void OnPreRender()
    {
        if (!bCommandBufferInitialized)
        {
            InitializeCommandBuffer();
        }

        m_ClearMaterial.SetTexture("_textureY", _videoTextureY);
        m_ClearMaterial.SetTexture("_textureCbCr", _videoTextureCbCr);

        m_ClearMaterial.SetMatrix("_DisplayTransform", _displayTransform);
    }
#endif
}
