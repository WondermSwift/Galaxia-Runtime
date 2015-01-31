﻿using System.Collections.Generic;
using UnityEngine;

namespace Galaxia
{
    public class ParticlesPrefab : ScriptableObject
    {
        #region Public
        #endregion
        #region Private
        [SerializeField]
        [HideInInspector]
        private bool m_active = true;
        [SerializeField]
        private int m_count = 100000;
        [SerializeField]
        private int m_seed = 0;
        [SerializeField]
        private float m_size = 0.1f;
        [SerializeField]
        private float m_maxScreenSize = 0.1f;
        [Header("Distribution")]
        [CurveRange(0, 0, 1, 1)]
        [SerializeField]
        private AnimationCurve m_positionDistribution = AnimationCurve.Linear(0,0,1,1);

        // Size -------------------------------------------
        [SerializeField]
        [HideInInspector]
        private DistributionProperty m_sizeDistributor = new DistributionProperty(DistrbuitionType.Linear,AnimationCurve.Linear(0, 1, 1, 1),0,1);

        // Alpha ------------------------------------------------
        [SerializeField]
        [HideInInspector]
        private DistributionProperty m_alphaDistributor = new DistributionProperty(DistrbuitionType.Linear, DefaultResources.AlphaCurve, 0, 1);

        // Color -------------------------------------------
        [SerializeField]
        [HideInInspector]
        public DistributionProperty m_colorDistributor = new DistributionProperty(DistrbuitionType.Linear, AnimationCurve.Linear(0, 1, 1, 1), 0, 1);
        [SerializeField]
        [HideInInspector]
        private Gradient m_color = DefaultResources.StarColorGradient;
        

        [Header("Rendering")]
        [SerializeField]
        private UnityEngine.Rendering.BlendMode m_blendModeSrc = UnityEngine.Rendering.BlendMode.One;
        [SerializeField]
        private UnityEngine.Rendering.BlendMode m_blendModeDis = UnityEngine.Rendering.BlendMode.One;
        [SerializeField]
        private int m_renderQueue = 0;
        [SerializeField]
        private Texture2D m_texture;

        #region hidden
        [SerializeField]
        [HideInInspector]
        private Preset m_originalPreset;
        [SerializeField]
        [HideInInspector]
        private Material m_material;
        [SerializeField]
        [HideInInspector]
        private GalaxyPrefab m_galaxyPrefab;
        #endregion
        #endregion

        public void SetUp(GalaxyPrefab prefab)
        {
            m_galaxyPrefab = prefab;
        }

        public Color GetColor(Vector3 pos,float distance, float GalaxySize, float angle,float index)
        {
            float colorPos = 0;
            float alphaPos = 0;
            switch (m_colorDistributor.Type)
            {
                case DistrbuitionType.Angle:
                    colorPos = (Mathf.Cos(angle) + 1) / 2f;
                    break;
                case DistrbuitionType.Distance:
                    colorPos = distance / GalaxySize;
                    break;
                case DistrbuitionType.Linear:
                    colorPos = (float)index / (float)Count;
                    break;
                case DistrbuitionType.Perlin:
                    colorPos = Mathf.PerlinNoise(pos.x / GalaxySize, pos.z / GalaxySize);
                    break;
                case DistrbuitionType.Random:
                    colorPos = Random.Next();
                    break;
            }

            switch (m_alphaDistributor.Type)
            {
                case DistrbuitionType.Angle:
                    alphaPos = (Mathf.Cos(angle) + 1) / 2f;
                    break;
                case DistrbuitionType.Distance:
                    alphaPos = distance / GalaxySize;
                    break;
                case DistrbuitionType.Linear:
                    alphaPos = (float)index / (float)Count;
                    break;
                case DistrbuitionType.Perlin:
                    colorPos = Mathf.PerlinNoise(pos.x, pos.z);
                    break;
                case DistrbuitionType.Random:
                    alphaPos = Random.Next();
                    break;
            }

            Color c = Color.Evaluate(Mathf.Lerp(colorPos,Random.Next(),m_colorDistributor.Variation)) * m_colorDistributor.Multiplayer;
            c.a *= Mathf.Lerp(m_alphaDistributor.DistributionCurve.Evaluate(alphaPos), Random.Next(), m_alphaDistributor.Variation) * m_alphaDistributor.Multiplayer * m_colorDistributor.DistributionCurve.Evaluate(colorPos);
            return c;
        }

        public float GetSize(Vector3 pos,float distance, float GalaxySize, float angle, float index)
        {
            float sizePos = 0;
            switch (m_sizeDistributor.Type)
            {
                case DistrbuitionType.Angle:
                    sizePos = (Mathf.Cos(angle) + 1) / 2f;
                    break;
                case DistrbuitionType.Distance:
                    sizePos = distance / GalaxySize;
                    break;
                case DistrbuitionType.Linear:
                    sizePos = (float)index / (float)Count;
                    break;
                case DistrbuitionType.Perlin:
                    sizePos = Mathf.PerlinNoise(pos.x, pos.z);
                    break;
                case DistrbuitionType.Random:
                    sizePos = Random.Next();
                    break;
            }

            float size = Mathf.Lerp(m_sizeDistributor.DistributionCurve.Evaluate(sizePos), Random.Next(), m_sizeDistributor.Variation) * Size;
            return size;
        }

        public float GetRotation()
        {
            return Random.Next() * Mathf.PI * 2;
        }

        public void UpdateMaterial(GalaxyPrefab prefab)
        {
            if (m_material == null)
            {
                m_material = new Material(prefab.shader);
                m_material.hideFlags = HideFlags.DontSave;
            }

            if (prefab != null && m_material != null)
            {
                m_material.mainTexture = Texture;
                m_material.SetFloat("GalaxySize", prefab.Size);
                m_material.SetFloat("MaxScreenSize", MaxScreenSize);
                m_material.SetFloat("Count", Count);
                m_material.SetInt("MySrcMode",(int)m_blendModeSrc);
                m_material.SetInt("MyDstMode", (int)m_blendModeDis);
                m_material.renderQueue = m_renderQueue;
            }
        }

        public void UpdateMaterialAnimation(GalaxyPrefab prefab,float speed,bool Animate)
        {
            if (prefab != null && m_material != null)
            {
                m_material.SetInt("Animate", Animate ? 1 : 0);
                m_material.SetFloat("AnimationSpeed", speed);
            }
        }

        public void DestoryPrefab()
        {
            GameObject.DestroyImmediate(m_material,true);
            GameObject.DestroyImmediate(this, true);
        }

        #region Getters and Setters
        public bool active { get { return m_active; } set { m_active = value; } }
        public int Count { get { return m_count; } set { m_count = value; } }
        public int Seed { get { return m_seed; } set { m_seed = value; } }
        public float Size { get { return m_size; } set { m_size = value; } }
        public float MaxScreenSize { get { return m_maxScreenSize; } set { m_maxScreenSize = value; } }
        public DistributionProperty SizeDistributor { get { return m_sizeDistributor; } set { m_sizeDistributor = value; } }
        public DistributionProperty AlphaDistributor { get { return m_alphaDistributor; } set { m_alphaDistributor = value; } }
        public DistributionProperty ColorDistributor { get { return m_colorDistributor; } set { m_colorDistributor = value; } }
        public AnimationCurve PositionDistribution { get { return m_positionDistribution; } set { m_positionDistribution = value; } }
        public Gradient Color { get { return m_color; } set { m_color = value; } }
        public Texture2D Texture { get { return m_texture; } set { m_texture = value; } }
        public Preset OriginalPreset { get { return m_originalPreset; } set { m_originalPreset = value; } }
        public Material Material { get { return m_material; } set { m_material = value; } }
        public GalaxyPrefab GalaxyPrefab { get { return m_galaxyPrefab; } }
        #endregion

        #region enums
        public enum Preset
        {
            None,
            SmallStars,
            BigStars,
            Dust
        }
        #endregion
    }
}
