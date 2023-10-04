#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TestItem : PropertyItem
{
	[System.Serializable]
	public class ClassTest
	{
		public bool a = false;
		public string b = "Test";
	}

	[System.Serializable]
	public struct StructTest
	{
		public bool a;
		public Eunm b;
		public Vector3 c;
		public ClassTest d;
	}

	public enum Eunm
	{
		A,
		B,
		C
	}

	[SaveLoad]
	private Vector3 Position
	{
		get { return transform.position; }
		set { transform.position = value; }
	}

	[SaveLoad]
	[SerializeField]
	private Quaternion quatTest = Quaternion.identity;

	[SaveLoad]
	[SerializeField]
	private bool wokenUp = false;

	[SaveLoad]
	[SerializeField]
	private float floatTest = 0f;

	[SaveLoad]
	[SerializeField]
	private int intTest = 0;

	[SaveLoad]
	[SerializeField]
	private ClassTest classTest = new ClassTest();

	[SaveLoad]
	[SerializeField]
	private StructTest structTest = new StructTest();

	[SaveLoad]
	[SerializeField]
	private int[] intArrTest = null;

	[SaveLoad]
	[SerializeField]
	private ClassTest[] classArrTest = null;

	[SaveLoad]
	[SerializeField]
	private Eunm enumTest = Eunm.A;

	[SaveLoad]
	private bool PropTest
	{ get { return intTest == 1; } }

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			wokenUp = true;

			classTest.a = true;
			classTest.b = "nice";

			structTest.a = true;
			structTest.b = Eunm.C;
			structTest.c = Vector3.one;

			structTest.d = new ClassTest();
			structTest.d.a = true;
			structTest.d.b = "nice";

			intArrTest = new int[5] { 0, 1, 2, 3, 4 };

			classArrTest = new ClassTest[]
			{
				new ClassTest(),
				new ClassTest()
			};

			classArrTest[0].a = true;
			classArrTest[1].b = "nice";

			enumTest = Eunm.B;

			floatTest = 1f;
			intTest = 1;
			quatTest = Quaternion.identity * Quaternion.Euler(Vector3.up * 90f);

			StateChanged();
		}
	}
}
