﻿/***********************************************************
* Copyright (C) 2018 6degrees.xyz Inc.
*
* This file is part of the 6D.ai Beta SDK and is not licensed
* for commercial use.
*
* The 6D.ai Beta SDK can not be copied and/or distributed without
* the express permission of 6degrees.xyz Inc.
*
* Contact developers@6d.ai for licensing requests.
***********************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SixDegrees
{
    public class SDMesh : MonoBehaviour
    {

        private class SDMeshBlock
        {
            public GameObject block = null;

            public int version = -1;

            public int meshVersion = -1;

            public int colliderVersion = -1;
        }

        public const int MeshLayer = 9;
        private int meshVersion = -1;
        public GameObject meshPrefab;
        public Material depthMaskMaterial;
        private float blockSize = 0f;
        private Dictionary<Vector3Int, SDMeshBlock> blocks;

        void Awake()
        {
            blocks = new Dictionary<Vector3Int, SDMeshBlock>();
        }

        void FixedUpdate()
        {
            if (!SDPlugin.IsSDKReady)
            {
                return; // will try later
            }

            int blockBufferSize = 0;
            int vertexBufferSize = 0;
            int faceBufferSize = 0;
            int newVersion = -1;
            unsafe
            {
                newVersion = SDPlugin.SixDegreesSDK_GetBlockMeshInfo(&blockBufferSize, &vertexBufferSize, &faceBufferSize);
            }
            if (newVersion > meshVersion && blockBufferSize > 0 && vertexBufferSize > 0 && faceBufferSize > 0)
            {
                if (meshVersion < 0)
                {
                    blockSize = SDPlugin.SixDegreesSDK_GetMeshBlockSize();
                }
                UpdateMesh(newVersion, blockBufferSize, vertexBufferSize, faceBufferSize);
            }
            else if (newVersion == 0 && meshVersion > 0 && blockBufferSize == 0)
            {
                // Mesh was reset after loading a new map
                ClearMesh();
            }
        }

        Vector3Int GetBlockCoords(Vector3 coords)
        {
            return new Vector3Int(Mathf.FloorToInt(coords.x / blockSize), Mathf.FloorToInt(coords.y / blockSize), Mathf.FloorToInt(coords.z / blockSize));
        }

        void UpdateMesh(int newVersion, int blockBufferSize, int vertexBufferSize, int faceBufferSize)
        {
            meshVersion = newVersion;

            int[] blockArray = new int[blockBufferSize];
            float[] vertexArray = new float[vertexBufferSize];
            int[] faceArray = new int[faceBufferSize];

            int fullBlocks = 0;

            unsafe
            {
                fixed (int* blockBufferPtr = &blockArray[0], faceBufferPtr = &faceArray[0])
                {
                    fixed (float* vertexBufferPtr = &vertexArray[0])
                    {
                        fullBlocks = SDPlugin.SixDegreesSDK_GetBlockMesh(blockBufferPtr, vertexBufferPtr, faceBufferPtr,
                                                                          blockBufferSize, vertexBufferSize, faceBufferSize);
                    }
                }
            }
            bool gotAllBlocks = (fullBlocks == blockBufferSize / 6);

            if (fullBlocks < 0)
            {
                Debug.Log("Error calling SixDegreesSDK_GetMeshBlocks(), will not update the mesh.");
                return;
            }
            else if (fullBlocks == 0)
            {
                Debug.Log("SixDegreesSDK_GetMeshBlocks() gave us an empty mesh, will not update.");
                return;
            }
            else if (!gotAllBlocks)
            {
                Debug.Log("SixDegreesSDK_GetMeshBlocks() returned " + fullBlocks + " full blocks, expected " + (blockBufferSize / 6));
                return;
            }

            int firstVertex = 0;
            int firstTriangle = 0;
            int normalsOffset = vertexBufferSize/2;

            // Update all the full blocks returned by the API
            for (int b = 0; b < blockBufferSize; b += 6)
            {
                // Transform block coordinates from 6D right-handed coordinates to Unity left-handed coordinates
                // By flipping the sign of Z
                Vector3Int blockCoords = new Vector3Int(blockArray[b], blockArray[b + 1], -blockArray[b + 2]);
                int vertexCount = blockArray[b + 3];
                int triangleCount = blockArray[b + 4];
                int blockVersion = blockArray[b + 5];

                SDMeshBlock currentBlock;

                if (!blocks.TryGetValue(blockCoords, out currentBlock))
                {
                    currentBlock = new SDMeshBlock();
                    blocks[blockCoords] = currentBlock;
                }

                currentBlock.meshVersion = meshVersion;

                // Update block if it is outdated
                if (currentBlock.version < blockVersion)
                {
                    currentBlock.version = blockVersion;

                    List<Vector3> vertices = new List<Vector3>();
                    List<Vector3> normals = new List<Vector3>();
                    List<int> triangles = new List<int>();
                    // copy vertices
                    for (int va = firstVertex; va < firstVertex + vertexCount; va++)
                    {
                        // Transform vertices from 6D right-handed coordinates to Unity left-handed coordinates
                        // By flipping the sign of Z
                        int pos = va * 3;
                        vertices.Add(new Vector3(vertexArray[pos], vertexArray[pos + 1], -vertexArray[pos + 2]));
                        int norm = normalsOffset + pos;
                        normals.Add(new Vector3(vertexArray[norm], vertexArray[norm + 1], -vertexArray[norm + 2]));
                    }

                    // copy faces
                    for (int fa = firstTriangle; fa < firstTriangle + triangleCount; fa++)
                    {
                        //block.triangles.Add(new Vector3Int(faceArray[fa] - firstVertex, faceArray[fa + 2] - firstVertex, faceArray[fa + 1] - firstVertex));
                        int f = fa * 3;
                        triangles.Add(faceArray[f] - firstVertex);
                        triangles.Add(faceArray[f + 2] - firstVertex);
                        triangles.Add(faceArray[f + 1] - firstVertex);
                    }

                    if (currentBlock.block == null)
                    {
                        currentBlock.block = Instantiate(meshPrefab);
                        currentBlock.block.transform.parent = gameObject.transform;
                    }

                    Mesh workmesh = currentBlock.block.GetComponent<MeshFilter>().mesh;
                    workmesh.Clear();
                    workmesh.vertices = vertices.ToArray();
                    workmesh.normals = normals.ToArray();
                    workmesh.triangles = triangles.ToArray();
                    MeshCollider meshCollider = currentBlock.block.GetComponent<MeshCollider>();
                    // update the collider less often for optimal performance
                    if (meshCollider && (currentBlock.colliderVersion < 0 || (currentBlock.colliderVersion + 10 < currentBlock.version)))
                    {
                        meshCollider.sharedMesh = workmesh;
                        currentBlock.colliderVersion = currentBlock.version;
                    }
                }
                firstVertex += vertexCount;
                firstTriangle += triangleCount;
            }

            // Clean up obsolete blocks
            foreach (Vector3Int blockCoords in blocks.Keys)
            {
                if (blocks[blockCoords].meshVersion != meshVersion)
                {
                    Destroy(blocks[blockCoords].block.GetComponent<MeshFilter>().mesh);
                    Destroy(blocks[blockCoords].block);
                    blocks.Remove(blockCoords);
                }
            }
        }

        public void ShowMesh()
        {
            Material baseMaterial = null;
            if (meshPrefab)
            {
                MeshRenderer meshRenderer = meshPrefab.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    baseMaterial = meshRenderer.material;
                }
            }
            if (!baseMaterial)
            {
                return;
            }
            foreach (SDMeshBlock block in blocks.Values)
            {
                if (block.block.GetComponent<MeshRenderer>())
                {
                    block.block.GetComponent<MeshRenderer>().material = baseMaterial;
                }
            }
        }

        public void HideMesh()
        {
            foreach (SDMeshBlock block in blocks.Values)
            {
                if (block.block.GetComponent<MeshRenderer>())
                {
                    block.block.GetComponent<MeshRenderer>().material = depthMaskMaterial;
                }
            }
        }

        void ClearMesh()
        {
            meshVersion = 0;
            foreach (Vector3Int blockPos in blocks.Keys)
            {
                Destroy(blocks[blockPos].block.GetComponent<MeshFilter>().mesh);
                Destroy(blocks[blockPos].block);
                blocks.Remove(blockPos);
            }
        }
    }
}

