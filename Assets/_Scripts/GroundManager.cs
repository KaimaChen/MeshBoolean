using UnityEngine;
using System.Collections.Generic;
using System;

#region Class Hole
class Hole
{
    public Vector3 position;
    public float radius;

    public Hole(Vector3 pos, float r)
    {
        position = pos;
        radius = r;
    }
}

class HoleCompareY : IComparer<Hole>
{
    public int Compare(Hole holeA, Hole holeB)
    {
        if (Mathf.Approximately(holeA.position.z, holeB.position.z))
            return 0;
        else if (holeA.position.z > holeB.position.z)
            return 1;
        else
            return -1;
    }
}
#endregion

#region Class AABB
public class AABB : IComparable<AABB>
{
    public float bottom;
    public float top;
    public float left;
    public float right;

    public AABB(float b, float t, float l, float r)
    {
        bottom = b;
        top = t;
        left = l;
        right = r;
    }

    public AABB(AABB other)
    {
        bottom = other.bottom;
        top = other.top;
        left = other.left;
        right = other.right;
    }

    public void UnionWith(AABB other)
    {
        bottom = Mathf.Min(bottom, other.bottom);
        top = Mathf.Max(top, other.top);
        left = Mathf.Min(left, other.left);
        right = Mathf.Max(right, other.right);
    }

    public bool IsContains(AABB other)
    {
        return (bottom <= other.bottom && top >= other.top && left <= other.left && right >= other.right);
    }

    public int CompareTo(AABB other) //按水平方向排序
    {
        if (Mathf.Approximately(left, other.left))
            return 0;
        if (left > other.left)
            return 1;
        else
            return -1;
    }
}
#endregion

/// <summary>
/// 地面管理器，主要用来管理孔洞生成
/// 在孔洞部分用密集的Grid来表示，其他部分用稀疏的Quad表示
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class GroundManager : MonoBehaviour
{
    public static GroundManager Instance = null;

    public float gridCellSize = 0.2f;//孔洞那里网格的单位大小（用来控制那部分网格密度）
    public float width = 10; //整个地面的宽度
    public float height = 10; //整个地面的高度（长度）

    MeshFilter mFilter = null;
    MeshCollider mCollider = null;
    Mesh mMesh = null;
    List<Hole> mHoles = new List<Hole>();
    List<AABB> mHoleGrids = new List<AABB>();

    void Awake()
    {
        Instance = this;
        mFilter = GetComponent<MeshFilter>();
        mCollider = GetComponent<MeshCollider>();
    }

    void Start()
    {
        mMesh = new Mesh();
        mFilter.mesh = mMesh;
        mCollider.sharedMesh = mMesh;

        //生成一个Quad初始地形，可删除改用自己的网格
        mMesh.Clear();
        AddQuad(new Vector2(0, 0), new Vector2(1, 1));
    }

    public void CreateHole(Vector3 pos, float radius)
    {
        pos = transform.InverseTransformPoint(pos);

        CreateHoleByScanXY(pos, radius);
    }

    public void ClearHoles()
    {
        mHoles.Clear();
        mMesh.Clear();
        AddQuad(new Vector2(0, 0), new Vector2(1, 1));
    }

    #region Create Mesh
    Vector2 PositionFilter(Vector2 pos)
    {
        float x = Mathf.Clamp01(pos.x);
        float y = Mathf.Clamp01(pos.y);
        return new Vector2(x, y);
    }

    /// <summary>
    /// 增加Quad网格
    /// </summary>
    /// <param name="start">左下角所在坐标[0,1]</param>
    /// <param name="end">右上角所在坐标[0,1]</param>
    void AddQuad(Vector2 start, Vector2 end)
    {
        if (end.x <= start.x || end.y <= start.y)
        {
            //Debug.LogWarning("In AddQuad function, the xy in end" + topRight + " should bigger than start" + bottomLeft);
            return;
        }

        start = PositionFilter(start);
        end = PositionFilter(end);

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(width * start.x, 0, height * start.y); //左下
        vertices[1] = new Vector3(width * start.x, 0, height * end.y); //左上
        vertices[2] = new Vector3(width * end.x, 0, height * end.y); //右上
        vertices[3] = new Vector3(width * end.x, 0, height * start.y); //右下

        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(start.x, start.y);
        uvs[1] = new Vector2(start.x, end.y);
        uvs[2] = new Vector2(end.x, end.y);
        uvs[3] = new Vector2(end.x, start.y);

        int[] triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
        int originVerticesCount = mMesh.vertices.Length;
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] += originVerticesCount;
        }

        List<Vector3> totalVertices = new List<Vector3>(mMesh.vertices);
        totalVertices.AddRange(vertices);

        List<Vector2> totalUVs = new List<Vector2>(mMesh.uv);
        totalUVs.AddRange(uvs);

        List<int> totalTriangles = new List<int>(mMesh.triangles);
        totalTriangles.AddRange(triangles);

        mMesh.vertices = totalVertices.ToArray();
        mMesh.uv = totalUVs.ToArray();
        mMesh.triangles = totalTriangles.ToArray();
        mMesh.RecalculateNormals();
        mMesh.RecalculateBounds();
        mCollider.sharedMesh = mMesh;
    }

    void GenerateGrid(Vector2 start, Vector2 end)
    {
        if (end.x <= start.x || end.y <= start.y)
        {
            //Debug.LogWarning("In AddGrid function, end should bigger than start");
            return;
        }

        start = PositionFilter(start);
        end = PositionFilter(end);

        float startX = width * start.x;
        float startY = height * start.y;
        float cellSizeX = (end.x - start.x) * gridCellSize;
        float cellSizeY = (end.y - start.y) * gridCellSize;
        float gridWidth = width * (end.x - start.x);
        float gridHeight = height * (end.y - start.y);
        int xSize = Mathf.FloorToInt(gridWidth / gridCellSize);
        int ySize = Mathf.FloorToInt(gridHeight / gridCellSize);
        float gridCellSizeX = gridWidth / xSize;
        float gridCellSizeY = gridHeight / ySize;

        Vector3[] vertices = new Vector3[(xSize + 1) * (ySize + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSize * ySize * 6];

        int v = 0;
        for (int row = 0; row < ySize + 1; row++)
        {
            for (int col = 0; col < xSize + 1; col++)
            {
                float x = startX + col * gridCellSizeX;
                float z = startY + row * gridCellSizeY;
                uvs[v] = new Vector2(x / width, z / height);
                vertices[v++] = new Vector3(x, 0, z);
            }
        }

        int t = 0;
        for (int row = 0; row < ySize; row++)
        {
            for (int col = 0; col < xSize; col++)
            {
                int bottomLeft = row * (xSize + 1) + col;
                int bottomRight = bottomLeft + 1;
                int topLeft = (row + 1) * (xSize + 1) + col;
                int topRight = topLeft + 1;
                triangles[t + 0] = bottomLeft;
                triangles[t + 1] = topLeft;
                triangles[t + 2] = topRight;
                triangles[t + 3] = bottomLeft;
                triangles[t + 4] = topRight;
                triangles[t + 5] = bottomRight;
                t += 6;
            }
        }

        int originVerticesCount = mMesh.vertices.Length;
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] += originVerticesCount;
        }

        List<Vector3> totalVertices = new List<Vector3>(mMesh.vertices);
        totalVertices.AddRange(vertices);

        List<Vector2> totalUVs = new List<Vector2>(mMesh.uv);
        totalUVs.AddRange(uvs);

        List<int> totalTriangles = new List<int>(mMesh.triangles);
        totalTriangles.AddRange(triangles);

        mMesh.vertices = totalVertices.ToArray();
        mMesh.uv = totalUVs.ToArray();
        mMesh.triangles = totalTriangles.ToArray();
    }

    void AddGrid(Vector2 start, Vector2 end)
    {
        GenerateGrid(start, end);

        mMesh.RecalculateNormals();
        mMesh.RecalculateBounds();
        mCollider.sharedMesh = mMesh;
    }

    void AddGridWithHole(Vector2 start, Vector2 end, Vector3 pos, float radius)
    {
        GenerateGrid(start, end);

        Vector3[] vertices = mMesh.vertices;
        float sqrRadius = radius * radius;
        for (int i = 0; i < vertices.Length; i++)
        {
            float sqrDistance = Vector3.SqrMagnitude(vertices[i] - pos);
            if (sqrDistance <= sqrRadius)
            {
                float percent = (sqrRadius - sqrDistance) / sqrRadius;
                float offset = Mathf.Lerp(0, radius, percent);
                vertices[i] = new Vector3(vertices[i].x, vertices[i].y - offset, vertices[i].z);
            }
        }

        mMesh.vertices = vertices;
        mMesh.RecalculateNormals();
        mMesh.RecalculateBounds();
        mCollider.sharedMesh = mMesh;
    }

    void AddGridWithHoles(AABB aabb, List<Hole> holes)
    {
        AddGridWithHoles(new Vector2(aabb.left, aabb.bottom), new Vector2(aabb.right, aabb.top), holes);
    }

    void AddGridWithHoles(Vector2 start, Vector2 end, List<Hole> holes)
    {
        GenerateGrid(start, end);

        Vector3[] vertices = mMesh.vertices;
        for (int holeIndex = 0; holeIndex < holes.Count; holeIndex++)
        {
            float radius = holes[holeIndex].radius;
            Vector3 pos = holes[holeIndex].position;

            float sqrRadius = radius * radius;
            for (int i = 0; i < vertices.Length; i++)
            {
                float sqrDistance = Vector3.SqrMagnitude(vertices[i] - pos);
                if (sqrDistance <= sqrRadius)
                {
                    float percent = (sqrRadius - sqrDistance) / (sqrRadius);
                    float offset = Mathf.Lerp(0, radius, percent);
                    vertices[i] = new Vector3(vertices[i].x, vertices[i].y - offset, vertices[i].z);
                }
            }
        }

        mMesh.vertices = vertices;
        mMesh.RecalculateNormals();
        mMesh.RecalculateBounds();
        mCollider.sharedMesh = mMesh;
    }
    #endregion

    #region Algorithm ScanXY
    class ScanXYData
    {
        public AABB totalAABB;
        public List<AABB> childrenAABB = new List<AABB>();

        public ScanXYData(AABB aabb)
        {
            totalAABB = aabb;
            childrenAABB = new List<AABB>();
        }

        public ScanXYData(AABB aabb, List<AABB> children)
        {
            totalAABB = aabb;
            childrenAABB = children;
        }

        public override string ToString()
        {
            return totalAABB.ToString();
        }
    }

    //首先沿Y方向扫描，把Y方向相交的存在一起ListY
    //将ListY里的沿X方向扫描，把X方向相交的组成大的AABB
    void CreateHoleByScanXY(Vector3 pos, float radius)
    {
        Hole hole = new Hole(pos, radius);
        mHoles.Add(hole);

        //如果新孔洞在已有孔洞Grid范围内，则直接操作顶点下沉而不重新生成网格
        for (int i = 0; i < mHoleGrids.Count; i++)
        {
            AABB target = CalculateAABB(hole);
            if (mHoleGrids[i].IsContains(target))
            {
                AddHole(pos, radius);
                return;
            }
        }

        mMesh.Clear();
        mHoleGrids.Clear();


        mHoles.Sort(new HoleCompareY());

        List<ScanXYData> xyDatas = new List<ScanXYData>();

        int holeIndex = 0;
        while (holeIndex < mHoles.Count)
        {
            AABB curAABB = CalculateAABB(mHoles[holeIndex]);
            List<AABB> rawChildrenAABB = new List<AABB>();
            rawChildrenAABB.Add(new AABB(curAABB));

            int count = 1;
            for (int nextHoleIndex = holeIndex + 1; nextHoleIndex < mHoles.Count; nextHoleIndex++)
            {
                AABB nextAABB = CalculateAABB(mHoles[nextHoleIndex]);

                if (curAABB.top >= nextAABB.bottom) //Y方向上相交
                {
                    count++;
                    curAABB.UnionWith(nextAABB);
                    rawChildrenAABB.Add(new AABB(nextAABB));
                }
                else
                {
                    break;
                }
            }
            holeIndex += count;

            List<AABB> childrenAABB = new List<AABB>();
            rawChildrenAABB.Sort(); //在X方向上排序
            //在X方向对子AABB做和Y方向类似的操作
            int rawAABBIndex = 0;
            while (rawAABBIndex < rawChildrenAABB.Count)
            {
                AABB current = new AABB(rawChildrenAABB[rawAABBIndex]);

                int aabbCount = 1;
                for (int nextAABBIndex = rawAABBIndex + 1; nextAABBIndex < rawChildrenAABB.Count; nextAABBIndex++)
                {
                    AABB next = rawChildrenAABB[nextAABBIndex];
                    if (current.right >= next.left) //X方向上相交
                    {
                        aabbCount++;
                        current.UnionWith(next);
                    }
                    else
                    {
                        break;
                    }
                }
                rawAABBIndex += aabbCount;
                childrenAABB.Add(current);
            }

            ScanXYData xyData = new ScanXYData(new AABB(curAABB));
            xyData.childrenAABB = childrenAABB;
            xyDatas.Add(xyData);
            mHoleGrids.AddRange(childrenAABB);
        }

        //根据数据创建网格
        ScanXYCreateMesh(xyDatas);
    }

    void ScanXYCreateMesh(List<ScanXYData> datas)
    {
        float bottom = 0;
        for (int i = 0; i < datas.Count; i++)
        {
            AABB aabb = NormalizeAABB(datas[i].totalAABB);
            List<AABB> childrenAABB = datas[i].childrenAABB;

            AddQuad(new Vector2(0, bottom), new Vector2(1, aabb.bottom)); //下

            for (int k = 0; k < childrenAABB.Count; k++)
            {
                AABB item = NormalizeAABB(childrenAABB[k]);

                AddGridWithHoles(item, mHoles); //孔洞Grid
                AddQuad(new Vector2(item.left, aabb.bottom), new Vector2(item.right, item.bottom)); //Y层面的下面
                AddQuad(new Vector2(item.left, item.top), new Vector2(item.right, aabb.top)); //Y层面的上面
            }

            for (int k = 0; k < childrenAABB.Count; k++)
            {
                AABB childAABB = datas[i].childrenAABB[k];
                float x = (k > 0) ? datas[i].childrenAABB[k - 1].right / width : 0;
                AddQuad(new Vector2(x, aabb.bottom), new Vector2(childAABB.left / width, aabb.top)); //X层面的左边
            }
            AABB lastAABB = childrenAABB[childrenAABB.Count - 1];
            AddQuad(new Vector2(lastAABB.right / width, aabb.bottom), new Vector2(1, aabb.top)); //X层面的右边

            bottom = aabb.top;
        }
        AddQuad(new Vector2(0, bottom), new Vector2(1, 1)); //最上
    }

    void AddHole(Vector3 pos, float radius)
    {
        Vector3[] vertices = mMesh.vertices;
        float sqrRadius = radius * radius;
        for (int i = 0; i < vertices.Length; i++)
        {
            float sqrDistance = Vector3.SqrMagnitude(vertices[i] - pos);
            if (sqrDistance <= sqrRadius)
            {
                float percent = (sqrRadius - sqrDistance) / sqrRadius;
                float offset = Mathf.Lerp(0, radius, percent);
                vertices[i] = new Vector3(vertices[i].x, vertices[i].y - offset, vertices[i].z);
            }
        }

        mMesh.vertices = vertices;
        mMesh.RecalculateNormals();
        mMesh.RecalculateBounds();
        mCollider.sharedMesh = mMesh;
    }

    AABB CalculateAABB(Vector3 pos, float radius)
    {
        float left = pos.x - radius;
        float bottom = pos.z - radius;
        float right = pos.x + radius;
        float top = pos.z + radius;

        return new AABB(bottom, top, left, right);
    }

    AABB CalculateAABB(Hole hole)
    {
        return CalculateAABB(hole.position, hole.radius);
    }

    AABB NormalizeAABB(AABB aabb)
    {
        return new AABB(aabb.bottom / height, aabb.top / height, aabb.left / width, aabb.right / width);
    }
    #endregion
}