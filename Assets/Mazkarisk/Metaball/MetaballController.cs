using System.Collections.Generic;
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

	// �������Z�p
	const float SPHERE_RADIUS = 0.02f;  // ���̕\����̔��a
	const float SPHERE_FORCE_RADIUS = SPHERE_RADIUS * 3f;  // ���̈��́E�˗͓��̉e�����a
	const int GRID_DIVISION = 64; // �O���b�h�̒P�ꎲ�����̕�����

	int sphereCount = 4096;
	GameObject[] sphereObjects = new GameObject[MAX_SPHERE_COUNT];
	Rigidbody[] sphereRigidbodies = new Rigidbody[MAX_SPHERE_COUNT];
	SphereCollider[] sphereColliders = new SphereCollider[MAX_SPHERE_COUNT];

	List<int>[] sortedSpheres = new List<int>[GRID_DIVISION * GRID_DIVISION * GRID_DIVISION];
	Vector3[] spherePositions = new Vector3[MAX_SPHERE_COUNT];
	Vector3[] sphereVelocities = new Vector3[MAX_SPHERE_COUNT];
	Vector3[] sphereAccelerations = new Vector3[MAX_SPHERE_COUNT];

	void Start() {
		GetComponent<MeshFilter>().sharedMesh = CreateMeshForFullScreenEffect();
		GetComponent<MeshRenderer>().material = material;

		// ���̐ݒ�
		for (var i = 0; i < sphereCount; i++) {
			sphereObjects[i] = new GameObject("MetaballSphere" + i);
			sphereObjects[i].transform.parent = transform;
			sphereColliders[i] = sphereObjects[i].AddComponent<SphereCollider>();
			sphereColliders[i].radius = SPHERE_RADIUS * 0.5f; // �����蔻��̔��a�͔����ɂ��Ă���
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
			_spheres[i] = new Vector4(position.x, position.y, position.z, SPHERE_RADIUS);
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
		Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		for (var i = 0; i < sphereCount; i++) {
			Vector3 position = sphereRigidbodies[i].position;

			// ���ȏ㗎�����{�[���̓��X�|�[��
			if (position.y < -10) {
				RespawnBall(i);
				position = sphereRigidbodies[i].position;
			}

			min = Vector3.Min(min, position);
			max = Vector3.Max(max, position);

			spherePositions[i] = position;
			sphereVelocities[i] = sphereRigidbodies[i].linearVelocity;
			sphereAccelerations[i] = Vector3.zero;
		}
		stopwatch.Stop();
		logText += "���X�|�[�������̎��� : " + stopwatch.Elapsed.TotalMilliseconds + " ms\n";

		// �O���b�h�̒��S�Ƒ傫�����Z�o
		Vector3 gridCenter = (min + max) * 0.5f;
		Vector3 wholeGridSize = Vector3.Max(max - min, Vector3.one * (SPHERE_FORCE_RADIUS * GRID_DIVISION)); // ��ׂ̃O���b�h�܂Ō���΍ςނ悤�ɃT�C�Y�𒲐�
		min = gridCenter - wholeGridSize * 0.5f;
		max = gridCenter + wholeGridSize * 0.5f;
		Vector3 singleGridSize = wholeGridSize / GRID_DIVISION;

		// �O���b�h������
		stopwatch.Restart();
		for (int i = 0; i < sortedSpheres.Length; i++) {
			if (sortedSpheres[i] == null) {
				sortedSpheres[i] = new List<int>();
			}
			sortedSpheres[i].Clear();
		}
		stopwatch.Stop();
		logText += "�O���b�h�����������̎��� : " + stopwatch.Elapsed.TotalMilliseconds + " ms\n";

		// �O���b�h�i�[����
		stopwatch.Restart();
		for (int i = 0; i < sphereCount; i++) {
			// �O���b�h���i�qID�̓���
			Vector3 gridPosition = spherePositions[i] - min;
			int x = Mathf.Clamp((int)(gridPosition.x / singleGridSize.x), 0, GRID_DIVISION - 1);
			int y = Mathf.Clamp((int)(gridPosition.y / singleGridSize.y), 0, GRID_DIVISION - 1);
			int z = Mathf.Clamp((int)(gridPosition.z / singleGridSize.z), 0, GRID_DIVISION - 1);
			int indexInGrid = GetIndexInGrid(x, y, z);

			// �O���b�h�ɒǉ�
			sortedSpheres[indexInGrid].Add(i);
		}
		stopwatch.Stop();
		logText += "�O���b�h�i�[�����̎��� : " + stopwatch.Elapsed.TotalMilliseconds + " ms\n";

		stopwatch.Restart();
		int maxLoopCount = 0;
		int actualPairCount = 0;
		for (int i = 0; i < sphereCount; i++) {
			// �O���b�h���i�qID�̓���
			Vector3 gridPosition = spherePositions[i] - min;
			int x = Mathf.Clamp((int)(gridPosition.x / singleGridSize.x), 0, GRID_DIVISION - 1);
			int y = Mathf.Clamp((int)(gridPosition.y / singleGridSize.y), 0, GRID_DIVISION - 1);
			int z = Mathf.Clamp((int)(gridPosition.z / singleGridSize.z), 0, GRID_DIVISION - 1);

			// �ΏۂƂȂ�i�q�͈̔�
			int xStart = Mathf.Clamp(x - 1, 0, GRID_DIVISION - 1);
			int yStart = Mathf.Clamp(y - 1, 0, GRID_DIVISION - 1);
			int zStart = Mathf.Clamp(z - 1, 0, GRID_DIVISION - 1);
			int xEnd = Mathf.Clamp(x + 1, 0, GRID_DIVISION - 1);
			int yEnd = Mathf.Clamp(y + 1, 0, GRID_DIVISION - 1);
			int zEnd = Mathf.Clamp(z + 1, 0, GRID_DIVISION - 1);

			// �i�q�̋��E����\���ɗ���Ă���ꍇ�́A���̕����̊i�q�͔���ΏۊO�Ƃ���B
			if (gridPosition.x > SPHERE_FORCE_RADIUS) {
				xStart = x;
			}
			if (gridPosition.y > SPHERE_FORCE_RADIUS) {
				yStart = y;
			}
			if (gridPosition.z > SPHERE_FORCE_RADIUS) {
				zStart = z;
			}
			if (gridPosition.x - singleGridSize.x < -SPHERE_FORCE_RADIUS) {
				xEnd = x;
			}
			if (gridPosition.y - singleGridSize.y < -SPHERE_FORCE_RADIUS) {
				yEnd = y;
			}
			if (gridPosition.z - singleGridSize.z < -SPHERE_FORCE_RADIUS) {
				zEnd = z;
			}

			for (int zIndex = zStart; zIndex <= zEnd; zIndex++) {
				for (int yIndex = yStart; yIndex <= yEnd; yIndex++) {
					for (int xIndex = xStart; xIndex <= xEnd; xIndex++) {
						int indexInGrid = GetIndexInGrid(xIndex, yIndex, zIndex);
						if (sortedSpheres[indexInGrid] == null) {
							continue;
						}
						for (int j = 0; j < sortedSpheres[indexInGrid].Count; j++) {
							maxLoopCount++;

							int myIndex = i;
							int otherIndex = sortedSpheres[indexInGrid][j];
							// �������g�͑ΏۊO�Ƃ���
							if (myIndex == otherIndex) {
								continue;
							}

							Vector3 diff = spherePositions[otherIndex] - spherePositions[myIndex];

							if (Mathf.Abs(diff.x) > SPHERE_FORCE_RADIUS) {
								continue;
							}
							if (Mathf.Abs(diff.y) > SPHERE_FORCE_RADIUS) {
								continue;
							}
							if (Mathf.Abs(diff.z) > SPHERE_FORCE_RADIUS) {
								continue;
							}

							float magnitude = diff.magnitude;
							if (magnitude > SPHERE_FORCE_RADIUS) {
								continue;
							}

							actualPairCount++;

							Vector3 normalized = diff.normalized;
							float influence = 1f - magnitude / SPHERE_FORCE_RADIUS;

							Vector3 acceleration = sphereAccelerations[myIndex];
							if (acceleration == null) {
								acceleration = Vector3.zero;
							}

							// ���͂��V�~�����[�g(�����悤�ɉ���������)
							acceleration += normalized * -(influence * influence * influence * influence * influence * influence) * 500f;

							// �\�ʒ��͂��V�~�����[�g(���������ɕۂ悤����������)
							if (magnitude > SPHERE_RADIUS * 2) {
								float intersurfaceStandardizedDistance = (magnitude - SPHERE_RADIUS * 2) / (SPHERE_FORCE_RADIUS - SPHERE_RADIUS * 2);
								acceleration += normalized * (intersurfaceStandardizedDistance * intersurfaceStandardizedDistance) * 2f;
							}

							// �S�����V�~�����[�g(���x����ł������悤�ɉ���������)
							Vector3 velocityDiff = sphereVelocities[otherIndex] - sphereVelocities[myIndex];
							acceleration += velocityDiff * influence * 20f;

							sphereAccelerations[myIndex] = acceleration;

						}
					}
				}
			}
		}
		stopwatch.Stop();
		logText += "���́E�˗͔��������̎���       : " + stopwatch.Elapsed.TotalMilliseconds + " ms\n";
		logText += "���́E�˗͔������̃y�A��     : " + maxLoopCount + " ��\n";
		logText += "���́E�˗͔��������������y�A�� : " + actualPairCount + " ��\n";

		// �����̓K�p
		stopwatch.Restart();
		for (int i = 0; i < sphereCount; i++) {
			Vector3 acceleration = sphereAccelerations[i];
			if (acceleration == null) {
				continue;
			}
			sphereRigidbodies[i].WakeUp();
			sphereRigidbodies[i].AddForce(acceleration, ForceMode.Acceleration);
		}
		stopwatch.Stop();
		logText += "�����̓K�p�����̎��� : " + stopwatch.Elapsed.TotalMilliseconds + " ms\n";
	}

	private void OnDrawGizmosSelected() {
		for (int i = 0; i < sphereCount; i++) {
			if (sphereObjects[i] == null) {
				continue;
			}

			Vector3 position = sphereObjects[i].transform.position;
			Vector3 acceleration = sphereAccelerations[i];
			if (acceleration == null) {
				continue;

			}
			Gizmos.color = Color.green;
			Gizmos.DrawLine(position, position + acceleration * 0.01f);

		}
	}

	private string logText = "";
	private void OnGUI() {
		string text = logText;

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
		sphereRigidbodies[index].mass = 4f / 3f * Mathf.PI * SPHERE_RADIUS * SPHERE_RADIUS * SPHERE_RADIUS * 1000f; // ���̖��x��1000kg/(m^3)�Ƃ��Čv�Z
		sphereRigidbodies[index].linearDamping = 1.5f;  // FixedTime�ݒ�ɂ���邪�AlinearDamping = 1.5f�̏ꍇ�A�I�[���x��6m/s���炢�ɂȂ�
		sphereRigidbodies[index].linearVelocity = Vector3.down * 6f + Random.insideUnitSphere;
		sphereRigidbodies[index].angularVelocity = Vector3.zero;
		sphereRigidbodies[index].interpolation = RigidbodyInterpolation.Interpolate;
	}

	private int GetIndexInGrid(int x, int y, int z) {
		return z * GRID_DIVISION * GRID_DIVISION + y * GRID_DIVISION + x;
	}
}
