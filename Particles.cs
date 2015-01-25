﻿//#define HIDE_SUB_ASSETS
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Linq;

namespace Galaxia
{
    [System.Serializable]
    public sealed class Particles : MonoBehaviour
    {
        #region Constants
        public const int MAX_VERTEX_PER_MESH = 65000;
        #endregion
        #region Private
        [SerializeField]
        [HideInInspector]
        private ParticlesPrefab m_prefab;
        [SerializeField]
        [HideInInspector]
        private Particle[] m_particleList;
        [SerializeField]
        [HideInInspector]
        private GameObject[] m_renderers;
        [SerializeField]
        [HideInInspector]
        private Mesh[] m_meshes;
        #endregion

        public void Generate(ParticlesPrefab Prefab, GalaxyPrefab galaxy)
        {
            Debug.Log("Particle Generation Started");
            this.m_prefab = Prefab;
            CreateParticleList(galaxy);
            CreateMeshes(galaxy);
        }

        public void Build(GalaxyPrefab galaxy,bool directx11)
        {
            if (m_prefab != null && m_prefab.active)
            {
                //only do geometry shader particles on direct x 10 and above
                if (directx11 && SystemInfo.graphicsShaderLevel >= 40)
                {
                    if (m_meshes != null)
                    {
                        DestroyRenderers();
                        m_renderers = new GameObject[m_meshes.Length];
                        m_prefab.RecreateMaterial(galaxy);

                        for (int i = 0; i < m_meshes.Length; i++)
                        {
                            GameObject g = new GameObject("Renderer", typeof(MeshRenderer), typeof(MeshFilter));
                            #if HIDE_SUB_ASSETS
                            g.hideFlags = HideFlags.HideInHierarchy;
                            #endif
                            //g.hideFlags |= HideFlags.DontSave;
                            g.transform.parent = transform;
                            g.GetComponent<MeshFilter>().sharedMesh = m_meshes[i];
                            g.renderer.sharedMaterial = m_prefab.Material;
                            g.renderer.castShadows = false;
                            g.renderer.receiveShadows = false;

                            m_renderers[i] = g;
                        }

                        
                    }
                }
                else
                {
                    GameObject g = new GameObject("Shuriken Renderer",typeof(ParticleSystem));
                    g.transform.parent = transform;
                    ParticleSystem system = g.GetComponent<ParticleSystem>();
                    system.maxParticles = m_prefab.Count;
                    system.playOnAwake = false;
                    system.renderer.material = Resources.Load<Material>("Materials/ParticleSystemParticle");
                    system.renderer.material.mainTexture = m_prefab.Texture;
                    system.SetParticles(ParticleList.Select(p => (ParticleSystem.Particle)p).ToArray(), m_particleList.Length);
                    system.Stop();
                }
                
            }
        }

        public void UpdateParticles(GalaxyPrefab galaxy)
        {
            if (m_prefab != null)
            {
                if (m_meshes != null && m_meshes.Length == MeshCount(m_prefab.Count))
                {
                    UpdateParticleList(galaxy);
                    UpdateMeshes(galaxy);
                }
                else
                {
                    Generate(m_prefab, galaxy);
                }
            }
            else
            {
                Debug.LogWarning("Prefab was deleted");
            }
        }

        public void UpdateParticles_MT(object galaxyObj)
        {
            GalaxyPrefab galaxy = galaxyObj as GalaxyPrefab;

            if (m_meshes != null && m_meshes.Length == MeshCount(m_prefab.Count))
            {
                UpdateParticleList(galaxy);
                UpdateMeshes(galaxy);
            }
            else
            {
                //Generate(Prefab, galaxy);
            }
        }

        public void DrawNow(GalaxyPrefab galaxy)
        {
            foreach(Mesh m in m_meshes)
            {
                if (m_prefab.active && m_prefab.Material != null)
                {
                    if (m_prefab.Material.SetPass(0))
                    {
                        m_prefab.UpdateMaterial(galaxy);
                        Graphics.DrawMeshNow(m, transform.parent.localToWorldMatrix);
                    }
                }
            }
        }

        public void Draw()
        {
            foreach (Mesh m in m_meshes)
            {
                if (m_prefab.active)
                {
                    Graphics.DrawMesh(m, transform.localToWorldMatrix, m_prefab.Material, 0);
                }
            }
        }

        int MeshCount(int Count)
        {
            return Mathf.FloorToInt((float)Count / (float)MAX_VERTEX_PER_MESH) + 1;
        }

        public void CreateMeshes(GalaxyPrefab galaxy)
        {
            DestoryMeshes();
            m_meshes = new Mesh[MeshCount(m_prefab.Count)];
            UpdateMeshes(galaxy);
        }

        void UpdateMeshes(GalaxyPrefab galaxy)
        {
            Random.seed = m_prefab.Seed;

            for (int i = 0; i < m_meshes.Length; i++)
            {
                int size = MAX_VERTEX_PER_MESH;
                if (i == m_meshes.Length - 1)
                    size = m_prefab.Count - MAX_VERTEX_PER_MESH * i;

                if (m_meshes[i] == null)
                {
                    m_meshes[i] = new Mesh();
                }
                else if (m_meshes[i].vertexCount > size)
                {
                    m_meshes[i].Clear(true);
                }

                Vector3[] vertex = new Vector3[size];
                Color[] color = new Color[size];
                Vector2[] info = new Vector2[size];
                Vector2[] indexStartTime = new Vector2[size];
                int[] indexes = new int[size];

                for (int e = 0; e < size; e++)
                {
                    vertex[e] = m_particleList[i * MAX_VERTEX_PER_MESH + e].position;
                    color[e] = m_particleList[i * MAX_VERTEX_PER_MESH + e].color;
                    info[e].x = m_particleList[i * MAX_VERTEX_PER_MESH + e].size;
                    info[e].y = m_particleList[i * MAX_VERTEX_PER_MESH + e].focalPoint;
                    indexStartTime[e].x = m_particleList[i * MAX_VERTEX_PER_MESH + e].index;
                    indexStartTime[e].y = m_particleList[i * MAX_VERTEX_PER_MESH + e].startingTime;
                    indexes[e] = e;
                }

                m_meshes[i].vertices = vertex;
                m_meshes[i].colors = color;
                m_meshes[i].uv = info;
                m_meshes[i].uv1 = indexStartTime;
                m_meshes[i].SetIndices(indexes, MeshTopology.Points, 0);
                m_meshes[i].RecalculateBounds();
            }
        }

        void UpdateMeshes_MT()
        {
            for (int i = 0; i < m_meshes.Length; i++)
            {
                int size = MAX_VERTEX_PER_MESH;
                if (i == m_meshes.Length - 1)
                    size = m_prefab.Count - MAX_VERTEX_PER_MESH * i;

                if (m_meshes[i] == null)
                {
                    m_meshes[i] = new Mesh();
                }
                else if (m_meshes[i].vertexCount > size)
                {
                    m_meshes[i].Clear(true);
                }

                Vector3[] vertex = new Vector3[size];
                Color[] color = new Color[size];
                Vector2[] info = new Vector2[size];
                Vector2[] indexStartTime = new Vector2[size];
                int[] indexes = new int[size];

                for (int e = 0; e < size; e++)
                {
                    vertex[e] = m_particleList[i * MAX_VERTEX_PER_MESH + e].position;
                    color[e] = m_particleList[i * MAX_VERTEX_PER_MESH + e].color;
                    info[e].x = m_particleList[i * MAX_VERTEX_PER_MESH + e].size;
                    info[e].y = Random.Next(-1.0f, 1.0f);
                    indexStartTime[e].x = (i * MAX_VERTEX_PER_MESH + e);
                    indexStartTime[e].y = Random.Next(0, Mathf.PI * 2f);
                    indexes[e] = e;
                }

                m_meshes[i].vertices = vertex;
                m_meshes[i].colors = color;
                m_meshes[i].uv = info;
                m_meshes[i].uv1 = indexStartTime;
                m_meshes[i].SetIndices(indexes, MeshTopology.Points, 0);
                m_meshes[i].RecalculateBounds();
            }
        }

        public void CreateParticleList(GalaxyPrefab galaxy)
        {
            m_particleList = new Particle[m_prefab.Count];
            UpdateParticleList(galaxy);
        }

        void UpdateParticleList(GalaxyPrefab galaxy)
        {
            if(galaxy.Distributor != null && m_prefab != null)
            {
                if (m_particleList.Length != m_prefab.Count)
                {
                    System.Array.Resize<Particle>(ref m_particleList, m_prefab.Count);
                }

                Random.seed = (int)(m_prefab.Seed);

                for (int i = 0; i < m_prefab.Count; i++)
                {
                    m_particleList[i] = new Particle();
                    galaxy.Distributor.Process(m_particleList[i], galaxy, m_prefab, 0, i);
                }
            }
            else
            {
                if(galaxy.Distributor == null)
                    Debug.LogWarning("No Particle Distributor");
                if (m_prefab == null)
                    Debug.LogWarning("No Particle Distributor");
                
            }
                
        }

        public void Destroy()
        {
            DestoryMeshes();
            DestroyRenderers();
            m_particleList = null;
            m_prefab = null;
            GameObject.DestroyImmediate(gameObject);
        }

        public void DestroyRenderers()
        {
            if (m_renderers != null)
            {
                foreach (GameObject renderer in m_renderers)
                {
                    if (renderer != null)
                    {
                        //GameObject.DestroyImmediate(renderer.GetComponent<MeshFilter>().sharedMesh, true);
                        GameObject.DestroyImmediate(renderer);
                    }
                }

                m_renderers = null;
            }
        }

        public void DestoryMeshes()
        {
            if (m_meshes != null)
            {
                foreach (Mesh m in m_meshes)
                {
                    DestroyImmediate(m, true);
                }

                m_meshes = null;
            }
        }

        #region Getters and setters
        public ParticlesPrefab Prefab { get { return m_prefab; } }
        public Particle[] ParticleList { get { return m_particleList; } }
        public GameObject[] Renderers { get { return m_renderers; } }
        public Mesh[] Meshes { get { return m_meshes; } }
        #endregion
    }
}