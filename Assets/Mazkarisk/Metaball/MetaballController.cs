using UnityEngine;

public class MetaballController : MonoBehaviour {

	private const int MAX_SPHERE_COUNT = 65536; // 球の最大個数（シェーダー側と合わせる）

	[SerializeField]
	private Material material = null;

	[SerializeField]
	float smoothWidth = 0.1f;
	[SerializeField]
	float maxStakeholdableDistance = 0.2f;
	[SerializeField, Range(1, 256)]
	int maxMarchCount = 256;
	[SerializeField]
	float hitThreshold = 0.001f;
	[SerializeField]
	bool debugViewEnabled = false;

	private readonly Vector4[] _spheres = new Vector4[MAX_SPHERE_COUNT];
	ComputeBuffer spheresBuffer = null;

	int sphereCount = 1024;
	GameObject[] sphereObjects = new GameObject[MAX_SPHERE_COUNT];
	Rigidbody[] sphereRigidbodies = new Rigidbody[MAX_SPHERE_COUNT];
	SphereCollider[] sphereColliders = new SphereCollider[MAX_SPHERE_COUNT];

	void Start() {
		GetComponent<MeshFilter>().sharedMesh = CreateMeshForFullScreenEffect();
		GetComponent<MeshRenderer>().material = material;

		// 球の設定
		for (var i = 0; i < sphereCount; i++) {
			float radius = 0.02f;

			sphereObjects[i] = new GameObject("MetaballSphere" + i);
			sphereObjects[i].transform.parent = transform;
			sphereRigidbodies[i] = sphereObjects[i].AddComponent<Rigidbody>();
			sphereColliders[i] = sphereObjects[i].AddComponent<SphereCollider>();
			sphereColliders[i].radius = radius * 0.5f; // 当たり判定の半径は半分にしておく

			RespawnBall(i);
		}

		int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));
		spheresBuffer = new ComputeBuffer(MAX_SPHERE_COUNT, stride, ComputeBufferType.Default);
	}

	void Update() {
		for (var i = 0; i < sphereCount; i++) {
			// 中心座標と半径を格納
			Vector3 position = sphereRigidbodies[i].position;
			float radius = sphereColliders[i].radius * 2f;
			_spheres[i] = new Vector4(position.x, position.y, position.z, radius);
		}
		spheresBuffer.SetData(_spheres);

		material.SetInt("_SphereCount", sphereCount);
		material.SetFloat("_SmoothWidth", smoothWidth);
		material.SetFloat("_MaxStakeholdableDistance", maxStakeholdableDistance);
		material.SetFloat("_MaxMarchCount", maxMarchCount);
		material.SetFloat("_MaxMarchDistance", Camera.main.farClipPlane * 0.5f);
		material.SetFloat("_HitThreshold", hitThreshold);
		material.SetInt("_DebugViewEnabled", debugViewEnabled ? 1 : 0);
		material.SetBuffer("_SpheresBuffer", spheresBuffer);
	}

	void FixedUpdate() {
		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		logText = "";

		// リスポーン処理と最大速度の調査
		sw.Restart();
		float maxVelocity = 0f;
		for (var i = 0; i < sphereCount; i++) {
			if (sphereRigidbodies[i].linearVelocity.magnitude > maxVelocity) {
				maxVelocity = sphereRigidbodies[i].linearVelocity.magnitude;
			}

			// 落ちたボールをリスポーン
			if (sphereRigidbodies[i].position.y < -10) {
				RespawnBall(i);
			}
		}
		sw.Stop();
		logText += "リスポーン処理の時間：" + sw.Elapsed.TotalMilliseconds + " ms\n";
	}

	private string logText = "";
	private void OnGUI() {
		// ログのテキストをスタイルに設定
		GUIStyle guiStyleBack = new GUIStyle();
		guiStyleBack.fontSize = 20;
		guiStyleBack.normal.textColor = Color.black;
		GUIStyle guiStyleFront = new GUIStyle();
		guiStyleFront.fontSize = 20;
		guiStyleFront.normal.textColor = Color.white;
		GUI.Label(new Rect(12, 12, Screen.width, Screen.height), logText, guiStyleBack);
		GUI.Label(new Rect(10, 10, Screen.width, Screen.height), logText, guiStyleFront);
	}

	/// <summary>
	/// フルスクリーンエフェクト用メッシュを新規作成する。
	/// </summary>
	/// <returns>新規作成されたフルスクリーンエフェクト用メッシュ</returns>
	private Mesh CreateMeshForFullScreenEffect() {
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

		return mesh;
	}

	void RespawnBall(int index) {
		Vector2 insideUnitCircle = Random.insideUnitCircle;

		Vector3 position = new Vector3(insideUnitCircle.x, Random.Range(10f, 50f), insideUnitCircle.y);
		Quaternion rotation = Random.rotation;

		sphereObjects[index].transform.SetLocalPositionAndRotation(position, rotation);
		sphereRigidbodies[index].linearDamping = 1.5f;  // FixedTime設定にもよるが、終端速度は6.3m/sくらいになる
		sphereRigidbodies[index].linearVelocity = Vector3.down * 6.3f;
		sphereRigidbodies[index].angularVelocity = Vector3.zero;
	}
}
