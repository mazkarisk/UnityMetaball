using System.Collections.Generic;
using System.Threading.Tasks;
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

	int sphereCount = 2048;
	GameObject[] sphereObjects = new GameObject[MAX_SPHERE_COUNT];
	Rigidbody[] sphereRigidbodies = new Rigidbody[MAX_SPHERE_COUNT];
	SphereCollider[] sphereColliders = new SphereCollider[MAX_SPHERE_COUNT];

	// �񓯊������p
	List<Vector3> spherePositions = null;
	List<float> sphereRadiuses = null;
	List<int[]> nearbyPairs = null;
	bool isListNearbyPairsRunning = false;

	void Start() {
		GetComponent<MeshFilter>().sharedMesh = CreateMeshForFullScreenEffect();
		GetComponent<MeshRenderer>().material = material;

		// ���̐ݒ�
		for (var i = 0; i < sphereCount; i++) {
			float radius = 0.02f;

			sphereObjects[i] = new GameObject("MetaballSphere" + i);
			sphereObjects[i].transform.parent = transform;
			sphereColliders[i] = sphereObjects[i].AddComponent<SphereCollider>();
			sphereColliders[i].radius = radius * 0.5f; // �����蔻��̔��a�͔����ɂ��Ă���
			sphereRigidbodies[i] = sphereObjects[i].AddComponent<Rigidbody>();
			RespawnBall(i);
		}

		int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));
		spheresBuffer = new ComputeBuffer(MAX_SPHERE_COUNT, stride, ComputeBufferType.Default);
	}

	void Update() {
		for (var i = 0; i < sphereCount; i++) {
			// ���S���W�Ɣ��a���i�[
			Vector3 position = sphereObjects[i].transform.position;
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
		System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
		logText = "";

		// ���X�|�[������
		stopwatch.Restart();
		List<Vector3> tempPositions = new List<Vector3>();
		List<float> tempRadiuses = new List<float>();
		for (var i = 0; i < sphereCount; i++) {
			Vector3 position = sphereRigidbodies[i].position;
			float radius = sphereColliders[i].radius * 2f;
			tempPositions.Add(position);
			tempRadiuses.Add(radius);
			// ���ȏ㗎�����{�[���̓��X�|�[��
			if (position.y < -10) {
				RespawnBall(i);
			}
		}
		stopwatch.Stop();
		logText += "���X�|�[�������̎��ԁF" + stopwatch.Elapsed.TotalMilliseconds + " ms\n";

		spherePositions = tempPositions;
		sphereRadiuses = tempRadiuses;

		// �񓯊������ŋߐڂ���y�A��T������
		Task.Run(ListNearbyPairs);

		if (nearbyPairs != null) {
			logText += "tempNearbyPairs : " + nearbyPairs.Count + "\n";
			stopwatch.Restart();

			List<int[]> tempNearbyPairs = new List<int[]>(nearbyPairs);
			for (int i = 0; i < tempNearbyPairs.Count; i++) {
				int indexA = tempNearbyPairs[i][0];
				int indexB = tempNearbyPairs[i][1];

				Vector3 positionA = sphereRigidbodies[indexA].position;
				Vector3 positionB = sphereRigidbodies[indexB].position;

				Vector3 diffAB = positionB - positionA;
				Vector3 directionAB = diffAB.normalized;
				float magnitude = diffAB.magnitude;
				float sumRadius = sphereRadiuses[indexA] + sphereRadiuses[indexB];

				if (magnitude < sumRadius * 2) {
					// ���́E�˗͂𔭐�������
					float x = magnitude / sumRadius;
					float f = 4f * (x - 1.5f) * (x - 1.5f) - 1f;
					sphereRigidbodies[indexA].AddForce(-directionAB * f * 0.02f, ForceMode.Acceleration);
					sphereRigidbodies[indexB].AddForce(directionAB * f * 0.02f, ForceMode.Acceleration);
				}
			}

			stopwatch.Stop();
			logText += "�ߐڃy�A�̏������ԁF" + stopwatch.Elapsed.TotalMilliseconds + " ms\n";
		}
	}

	private string logText = "";
	private void OnGUI() {

		string text = logText + logTextListNearbyPairs;

		// ���O�̃e�L�X�g�X�^�C����ݒ�
		GUIStyle guiStyleBack = new GUIStyle();
		guiStyleBack.fontSize = 20;
		guiStyleBack.normal.textColor = Color.black;
		GUIStyle guiStyleFront = new GUIStyle();
		guiStyleFront.fontSize = 20;
		guiStyleFront.normal.textColor = Color.white;

		// ��ʏ�Ƀ��O�o��
		GUI.Label(new Rect(12, 12, Screen.width, Screen.height), text, guiStyleBack);
		GUI.Label(new Rect(10, 10, Screen.width, Screen.height), text, guiStyleFront);
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

		sphereRigidbodies[index].position = position;
		sphereRigidbodies[index].rotation = rotation;
		sphereRigidbodies[index].linearDamping = 1.5f;  // FixedTime�ݒ�ɂ���邪�A�I�[���x��6m/s���炢�ɂȂ�
		sphereRigidbodies[index].linearVelocity = Vector3.down * 6f;
		sphereRigidbodies[index].angularVelocity = Vector3.zero;
		sphereRigidbodies[index].interpolation = RigidbodyInterpolation.Interpolate;
	}

	string logTextListNearbyPairs = "";
	/// <summary>
	/// �񓯊������ŋߐڂ���y�A��T������
	/// </summary>
	private async Task ListNearbyPairs() {
		// ��d�N���h�~
		if (isListNearbyPairsRunning) {
			return;
		}
		isListNearbyPairsRunning = true;

		System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
		stopwatch.Restart();
		List<Vector3> tempSpherePositions = new List<Vector3>(spherePositions);
		List<float> tempSphereRadiuses = new List<float>(sphereRadiuses);
		List<int[]> tempNearbyPairs = new List<int[]>();
		for (var i = 0; i < tempSpherePositions.Count; i++) {
			for (var j = i; j < tempSpherePositions.Count; j++) {
				Vector3 diff = tempSpherePositions[i] - tempSpherePositions[j];
				float sumRadius = tempSphereRadiuses[i] + tempSphereRadiuses[j];
				float sqrDistance = (sumRadius * 2) * (sumRadius * 2);
				if (diff.sqrMagnitude <= sqrDistance) {
					tempNearbyPairs.Add(new int[] { i, j });
				}
			}
		}
		nearbyPairs = tempNearbyPairs;
		stopwatch.Stop();

		// ���O���X�V
		logTextListNearbyPairs = "ListNearbyPairs : " + stopwatch.Elapsed.TotalMilliseconds + " ms\n";

		isListNearbyPairsRunning = false;
	}
}
