using UnityEngine;

[ExecuteAlways]
public class MetaballController : MonoBehaviour {
	void Start() {
		// フルスクリーン描画用ポリゴンを作成
		Vector3[] vertices = new Vector3[4];
		vertices[0] = new Vector3(-1, -1, 0);
		vertices[1] = new Vector3(1, -1, 0);
		vertices[2] = new Vector3(-1, 1, 0);
		vertices[3] = new Vector3(1, 1, 0);

		Vector2[] uv = new Vector2[4];
		uv[0] = new Vector2(0, 1);
		uv[1] = new Vector2(1, 1);
		uv[2] = new Vector2(0, 0);
		uv[3] = new Vector2(1, 0);

		int[] triangles = new int[] { 0, 1, 2, 2, 1, 3 };

		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.bounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		GetComponent<MeshFilter>().sharedMesh = mesh;
	}

	void Update() {

	}
}
