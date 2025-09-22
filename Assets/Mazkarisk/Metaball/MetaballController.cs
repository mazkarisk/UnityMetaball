using UnityEngine;

public class MetaballController : MonoBehaviour {

	private const int MAX_SPHERE_COUNT = 65536; // ���̍ő���i�V�F�[�_�[���ƍ��킹��j

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

		// ���̐ݒ�
		for (var i = 0; i < sphereCount; i++) {
			float radius = 0.02f;

			sphereObjects[i] = new GameObject("MetaballSphere" + i);
			sphereObjects[i].transform.parent = transform;
			sphereRigidbodies[i] = sphereObjects[i].AddComponent<Rigidbody>();
			sphereColliders[i] = sphereObjects[i].AddComponent<SphereCollider>();
			sphereColliders[i].radius = radius * 0.5f; // �����蔻��̔��a�͔����ɂ��Ă���

			RespawnBall(i);
		}

		int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));
		spheresBuffer = new ComputeBuffer(MAX_SPHERE_COUNT, stride, ComputeBufferType.Default);
	}

	void Update() {
		for (var i = 0; i < sphereCount; i++) {
			// ���S���W�Ɣ��a���i�[
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

		// ���X�|�[�������ƍő呬�x�̒���
		sw.Restart();
		float maxVelocity = 0f;
		for (var i = 0; i < sphereCount; i++) {
			if (sphereRigidbodies[i].linearVelocity.magnitude > maxVelocity) {
				maxVelocity = sphereRigidbodies[i].linearVelocity.magnitude;
			}

			// �������{�[�������X�|�[��
			if (sphereRigidbodies[i].position.y < -10) {
				RespawnBall(i);
			}
		}
		sw.Stop();
		logText += "���X�|�[�������̎��ԁF" + sw.Elapsed.TotalMilliseconds + " ms\n";
	}

	private string logText = "";
	private void OnGUI() {
		// ���O�̃e�L�X�g���X�^�C���ɐݒ�
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
	/// �t���X�N���[���G�t�F�N�g�p���b�V����V�K�쐬����B
	/// </summary>
	/// <returns>�V�K�쐬���ꂽ�t���X�N���[���G�t�F�N�g�p���b�V��</returns>
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
		sphereRigidbodies[index].linearDamping = 1.5f;  // FixedTime�ݒ�ɂ���邪�A�I�[���x��6.3m/s���炢�ɂȂ�
		sphereRigidbodies[index].linearVelocity = Vector3.down * 6.3f;
		sphereRigidbodies[index].angularVelocity = Vector3.zero;
	}
}
