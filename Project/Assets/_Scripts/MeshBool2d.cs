using Pathfinding.ClipperLib;
using Pathfinding.Poly2Tri;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshBool2d : MonoBehaviour 
{
	/// <summary>
	/// Clipper使用的是整数，所以要将Unity的Vector浮点转换为整数
	/// </summary>
	private const float k_precision = 100000;

	public float m_circleRadius = 1;
	public int m_circleVerticesCount = 30;

	private Mesh m_mesh;

	protected readonly List<List<IntPoint>> m_polys = new List<List<IntPoint>>()
	{
		new List<IntPoint>()
		{
			Convert(0, 0),
			Convert(10, 0),
			Convert(10, 10),
			Convert(0, 10),
		}
	};

	private readonly PolyTree m_polyTree = new PolyTree();

	private void Start()
    {
		m_mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = m_mesh;

		InitMesh();
	}

	private void Update()
    {
        if (Input.GetMouseButton(0))
        {
			Vector2 center = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			RemoveCircle(center, m_circleRadius, m_circleVerticesCount);
        }
    }

	private void InitMesh()
    {
		Clipper clipper = new Clipper(Clipper.ioStrictlySimple);
		clipper.AddPolygons(m_polys, PolyType.ptSubject);
		clipper.Execute(ClipType.ctDifference, m_polyTree);
		Clipper.SimplifyPolygons(m_polys);
		List<DelaunayTriangle> triangles = GenerateTriangles();
		GenerateMesh(triangles);
	}

	private void RemoveCircle(Vector2 center, float radius, int count = 40)
    {
		List<IntPoint> poly = new List<IntPoint>();

		float delta = Mathf.PI * 2 / count;
		for(int i = 0; i < count; i++)
        {
			float x = center.x + Mathf.Cos(delta * i) * radius;
			float y = center.y + Mathf.Sin(delta * i) * radius;
			poly.Add(Convert(x, y));
        }

		Clipper clipper = new Clipper(Clipper.ioStrictlySimple);
		clipper.AddPolygons(m_polys, PolyType.ptSubject);
		clipper.AddPolygon(poly, PolyType.ptClip);
		clipper.Execute(ClipType.ctDifference, m_polyTree);

		Clipper.PolyTreeToPolygons(m_polyTree, m_polys);
		Clipper.SimplifyPolygons(m_polys);

		List<DelaunayTriangle> triangles = GenerateTriangles();
		GenerateMesh(triangles);
    }

	private List<DelaunayTriangle> GenerateTriangles()
    {
		List<DelaunayTriangle> triangles = new List<DelaunayTriangle>();

		Dictionary<PolyNode, Polygon> dict = new Dictionary<PolyNode, Polygon>();
		PolyNode curt = m_polyTree.GetFirst();
		while(curt != null)
        {
			var polygon = Convert(curt.Contour);
			dict.Add(curt, polygon);

			if (curt.IsHole && curt.Parent != null)
				dict[curt.Parent].AddHole(polygon);

			curt = curt.GetNext();
        }

		foreach(var pair in dict)
        {
			var node = pair.Key;
			var poly = pair.Value;

			if (node.IsHole == false)
            {
				P2T.Triangulate(poly);
				triangles.AddRange(poly.Triangles);
			}
        }

		return triangles;
	}

	protected virtual void GenerateMesh(List<DelaunayTriangle> triangles)
    {
		m_mesh.Clear();

		Vector3[] vertices = new Vector3[triangles.Count * 3];
		for(int i = 0, index = 0; i < triangles.Count; i++)
        {
			vertices[index++] = Convert(triangles[i].Points._0);
			vertices[index++] = Convert(triangles[i].Points._2);
			vertices[index++] = Convert(triangles[i].Points._1);
        }
		m_mesh.vertices = vertices;

		int[] triIndices = new int[vertices.Length];
		for(int i = 0; i < vertices.Length; i++)
        {
			triIndices[i] = i;
        }
		m_mesh.triangles = triIndices;

		m_mesh.RecalculateNormals();
    }

	public static IntPoint Convert(float x, float y)
    {
		return new IntPoint(x * k_precision, y * k_precision);
    }

	public static Polygon Convert(List<IntPoint> list)
	{
		List<PolygonPoint> result = new List<PolygonPoint>();

		Clipper.SimplifyPolygon(list);
		for (int i = 0; i < list.Count; i++)
			result.Add(new PolygonPoint(list[i].X, list[i].Y));

		return new Polygon(result);
	}

	public static Vector2 Convert(IntPoint p)
	{
		return new Vector2(p.X / k_precision, p.Y / k_precision);
	}

	public static Vector2 Convert(TriangulationPoint p)
    {
		return new Vector2(p.Xf / k_precision, p.Yf / k_precision);
    }
}
