
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public struct RigidbodySettings
{
	public float mass;
	public float drag;
	public float angularDrag;

	public RigidbodySettings(Rigidbody rigidbody)
	{
		mass = rigidbody.mass;
		drag = rigidbody.drag;
		angularDrag = rigidbody.angularDrag;
	}

	public void Apply(Rigidbody rigidbody)
	{
		rigidbody.mass = mass;
		rigidbody.drag = drag;
		rigidbody.angularDrag = angularDrag;
	}
}

[SelectionBase]
public class Item : MonoBehaviour
{
	public static bool FindNearestParentItem(Collider collider, out Item item)
	{ return FindNearestParentItem(collider.transform, out item); }

	public static bool FindNearestParentItem(Transform transform, out Item item)
	{
		item = null;

		if (transform != null)
			item = transform.GetComponentInParent<Item>();

		return item != null;
	}

	#region PARENTING
	[SerializeField]
	private Item parentItem = null;
	public Item ParentItem => parentItem;

	private List<Item> childItems = new List<Item>();
	public List<Item> ChildItems => childItems;

	public delegate void ItemChildEvent(Item parentItem, Item childItem);

	public event ItemChildEvent OnChildAdded = delegate {};
	public event ItemChildEvent OnChildRemoved = delegate {};

	public void SetParent(Item setParentItem)
	{
		if(setParentItem == this)
		{
			Debug.LogError("Trying to set item as it's own parent. This is not allowed");
			return;
		}

		if(parentItem != null)
			parentItem.RemoveChild(this);

		parentItem = setParentItem;

		if (parentItem != null)
			parentItem.AddChild(this);
	}

	private void AddChild(Item childItem)
	{
		bool newChild = !childItems.Contains(childItem);

		childItems.Add(childItem); // allow double adds so double removes needed to remove child

		if(newChild)
			OnChildAdded(this, childItem);
	}

	private void RemoveChild(Item childItem)
	{
		if (childItems.Remove(childItem))
		{
			bool fullyRemoved = !childItems.Contains(childItem);
			if (fullyRemoved)
				OnChildRemoved(this, childItem);
		}
	}

	public Item GetRootItem()
	{
		Item rootItem = this;

		while(rootItem.ParentItem != null)
			rootItem = rootItem.ParentItem;

		return rootItem;
	}
	#endregion // PARENTING

	[SerializeField]
	private ItemData itemData = null;
	public ItemData Data => itemData;

	public LensManagerColor ColorLens = new LensManagerColor(requests => LensUtils.Priority(requests));

	public Color CurrentColor
	{
		get
		{
			if (ColorLens.GetRequestCount > 0)
				return ColorLens;

			if (itemData != null)
				return itemData.color;

			return Color.white;
		}
	}

	public LensManagerString NameLens = new LensManagerString(requests => LensUtils.Priority(requests));

	public string CurrentName
	{
		get
		{
			if (NameLens.GetRequestCount > 0)
				return NameLens;

			if (itemData != null)
				return itemData.Name;

			return gameObject.name;
		}
	}

#if UNITY_EDITOR
	//[Button("Find Or Create Item Data")]
	public void FindOrCreateItemData()
	{ itemData = ItemData.CreateItemData("Assets/Resources/ItemDatas", gameObject.name); }
#endif

	#region Properties
	private Dictionary<Type, PropertyItem> typeToPropertyMap = new Dictionary<Type, PropertyItem>();
	public Dictionary<Type, PropertyItem> TypeToPropertyMap => typeToPropertyMap;

	public bool TryGetProperty<T>(out T property) where T : PropertyItem
	{
		property = GetProperty<T>();
		return property != null;
	}

	public void RegisterProperty(PropertyItem propertyItem)
	{
		typeToPropertyMap.Add(propertyItem.GetType(), propertyItem);

		propertyItem.OnStateChanged += PropertyItem_OnStateChanged;

		OnPropertyItemAdded(propertyItem);
	}

	public void DeregisterProperty(PropertyItem propertyItem)
	{
		typeToPropertyMap.Remove(propertyItem.GetType());

		propertyItem.OnStateChanged -= PropertyItem_OnStateChanged;

		OnPropertyItemRemoved(propertyItem);
	}

	public T GetProperty<T>() where T : PropertyItem
	{ return GetComponent<T>(); }

	public PropertyItem GetProperty(Type type)
    { return GetComponent(type) as PropertyItem; }

	public PropertyItem GetOrAddProperty(Type type)
	{
		PropertyItem property = GetComponent(type) as PropertyItem;
		if (property == null)
			property = gameObject.AddComponent(type) as PropertyItem;

		return property;
	}

	public T GetOrAddProperty<T>() where T : PropertyItem
	{
		T property = GetComponent<T>();
		if (property == null)
			property = gameObject.AddComponent<T>();

		return property;
	}

	public bool HasProperty<T>() where T : PropertyItem
	{ return GetComponent<T>() != null; }

	public bool HasPropertyEnabled<T>() where T : PropertyItem
	{ 
		var prop = GetComponent<T>();
		return prop != null && prop.enabled; 
	}

	#endregion // Properties

	#region Physics
	[SerializeField]
	protected new Rigidbody rigidbody = null;
	public Rigidbody Rigidbody => rigidbody;

	[SerializeField]
	private bool defaultUseGravity = true;

	[SerializeField]
	private bool defaultKinematic = false;

	[SerializeField]
	private bool manualGravity = false;

	public LensManagerBool UseGravityLens = null;
	public LensManagerBool KinematicLens = null;
	public LensManager<CollisionDetectionMode> CollisionModeLens = null;
	public LensManager<RigidbodyConstraints> ConstraintsLens = null;

	public LensManagerFloat DragLens = null;
	public LensManagerFloat AngularDragLens = null;

	[SerializeField]
	private List<Collider> allColliders = new List<Collider>();
	public List<Collider> AllColliders => allColliders;

	//[ShowNonSerializedField, ReadOnly]
	private Bounds localBounds = new Bounds();
	public Bounds LocalBounds => localBounds;

	public Bounds CalculateWorldBounds(bool includeTriggers = false)
	{ 
		Bounds bounds = PhysicsUtils.CalculateCollidersBounds(AllColliders, includeTriggers);
		if (bounds.size == Vector3.zero && bounds.center == Vector3.zero)
			bounds.center = transform.position;

		return bounds;
	}

	public Vector3 Center
	{ get { return transform.TransformPoint(localBounds.center); } }

	//[ShowNativeProperty()]
	public float Height
	{ get { return localBounds.size.y; } }

	//[ShowNativeProperty()]
	public float Radius
	{ get { return Mathf.Max(localBounds.extents.x, localBounds.extents.z); } }

	//[ShowNativeProperty()]
	public float Volume
	{ get { return localBounds.size.x * localBounds.size.y * localBounds.size.z; } }

	public Vector3 GetWorldBoundsPos(Vector3 normalizedBoundsPos)
	{
		Vector3 offset = normalizedBoundsPos - Vector3.one * 0.5f;
		offset.Scale(localBounds.size);

		Vector3 boundsPos = localBounds.center + offset;
		Vector3 worldPos = transform.TransformPoint(boundsPos);

		return worldPos;
	}

	public Vector3 GetNearestPointOnBounds(Vector3 pos)
	{
		Vector3 localPos = transform.InverseTransformPoint(pos);
		Vector3 boundsPos = localBounds.ClosestPoint(localPos);
		return transform.TransformPoint(boundsPos);
	}

	private RigidbodySettings backupSettings = default;
	public RigidbodySettings BackupSettings => backupSettings;

	public RigidbodySettings RigidbodySettings
	{
		get { return rigidbody != null ? new RigidbodySettings(rigidbody) : backupSettings; }

		set
		{
			if (rigidbody != null)
				value.Apply(rigidbody);
			else
				backupSettings = value;
		}
	}

	#endregion // Physics

	#region IgnoredColliders
#if UNITY_EDITOR
	[SerializeField]
	private List<Collider> ignoredColliders = new List<Collider>();
#else
	private HashSet<Collider> ignoredColliders = new HashSet<Collider>();
#endif

	public bool IsColliderIgnored(Collider collider)
	{ return ignoredColliders.Contains(collider); }

	public void SetIgnoreCollider(Collider otherCollider, bool setIgnore = true)
    {
		foreach(var collider in allColliders)
			Physics.IgnoreCollision(collider, otherCollider, setIgnore);

		if (setIgnore)
			_AddIgnoredCollider(otherCollider);
		else
			_RemoveIgnoredCollider(otherCollider);
    }

	public void SetIgnoreColliders(IEnumerable<Collider> otherColliders, bool setIgnore = true)
	{
		foreach(var collider in allColliders)
		{
			foreach (var otherCollider in otherColliders)
			{
				_IgnoreCollision(collider, otherCollider, setIgnore);
			}
		}

		if (setIgnore)
			_AddIgnoredColliders(otherColliders);
		else
			_RemoveIgnoredColliders(otherColliders);
	}

	private void _IgnoreCollision(Collider a, Collider b, bool setIgnore = true)
	{
		if (a != null && b != null)
			Physics.IgnoreCollision(a, b, setIgnore);
	}

	private void _AddIgnoredCollider(Collider addCollider)
	{ ignoredColliders.Add(addCollider); }

	private bool _RemoveIgnoredCollider(Collider removeCollider)
	{ return ignoredColliders.Remove(removeCollider); }

	private void _AddIgnoredColliders(IEnumerable<Collider> addColliders)
	{
		foreach (var addCollider in addColliders)
			_AddIgnoredCollider(addCollider);
	}

	private bool _RemoveIgnoredColliders(IEnumerable<Collider> removeColliders)
	{
		bool removed = false;
		foreach (var removeCollider in removeColliders)
		{
			if (_RemoveIgnoredCollider(removeCollider))
				removed = true;
		}
		return removed;
	}

	private static List<Collider> ignoreAColliders = new List<Collider>();
	private static List<Collider> ignoreBColliders = new List<Collider>();

	public static void SetIgnoreCollisionBetweenItems(Item aItem, Item bItem, bool setIgnored = true)
	{
		if (aItem == null || bItem == null)
			return;

		ignoreAColliders.Clear();
		ignoreBColliders.Clear();

		for (int j = 0; j < aItem.AllColliders.Count; j++)
		{
			Collider aCollider = aItem.AllColliders[j];
			if (aCollider == null)
				continue;

			ignoreAColliders.Add(aCollider);
		}

		for (int k = 0; k < bItem.AllColliders.Count; k++)
		{
			Collider bCollider = bItem.AllColliders[k];
			if (bCollider == null)
				continue;

			ignoreBColliders.Add(bCollider);
		}

		for (int i = 0; i < ignoreAColliders.Count; i++)
		{
			Collider aCollider = ignoreAColliders[i];

			for (int j = 0; j < ignoreBColliders.Count; j++)
			{
				Collider bCollider = ignoreBColliders[j];
				Physics.IgnoreCollision(aCollider, bCollider, setIgnored);
			}
		}

		if (setIgnored)
		{
			aItem._AddIgnoredColliders(ignoreBColliders);
			bItem._AddIgnoredColliders(ignoreAColliders);
		}
		else
		{
			aItem._RemoveIgnoredColliders(ignoreBColliders);
			bItem._RemoveIgnoredColliders(ignoreAColliders);
		}
	}
	#endregion

	public event Action<Item, Bounds> OnBoundsChanged = delegate { };
	public event Action<Item> OnCollidersChanged = delegate { };

	public event Action<Item, Rigidbody> OnRigidbodyAdded = delegate { };
	public event Action<Item, Rigidbody> OnWillRemoveRigidbody = delegate { };
	public event Action<Item> OnRigidbodyRemoved = delegate { };

	public event Action<PropertyItem> OnPropertyItemAdded = delegate { };
	public event Action<PropertyItem> OnPropertyItemRemoved = delegate { };

	public event Action<Item, PropertyItem> OnStateChanged = delegate { };

	public event Action<Item> OnItemEnabled = delegate { };
	public event Action<Item> OnItemDisabled = delegate { };

	private bool beingDestroyed = false;
	public bool BeingDestroyed => beingDestroyed;

	public event Action<Item> OnItemDestroyed = delegate { };

	private bool collidersDirty = false;

	private bool hasAwoken = false;

	public void Awake()
	{
		if (hasAwoken)
			return;

		hasAwoken = true;

		AwakeIfNeeded();
	}

	protected virtual void AwakeIfNeeded()
	{
		if (parentItem != null)
			SetParent(parentItem); // make sure events get fired

		RefreshColliders();
		
		float defaultDrag = 0f;
		float defaultAngularDrag = 0.05f;

		CollisionDetectionMode defaultCollisionMode = CollisionDetectionMode.Discrete;
		RigidbodyConstraints defaultConstraints = RigidbodyConstraints.None;

		if (rigidbody != null)
		{
			defaultDrag = rigidbody.drag;
			defaultAngularDrag = rigidbody.angularDrag;

			defaultCollisionMode = rigidbody.collisionDetectionMode;
			defaultConstraints = rigidbody.constraints;

			backupSettings = new RigidbodySettings(rigidbody);
		}
		else
		{
			backupSettings = new RigidbodySettings()
			{ 
				mass = 1f, 
				drag = 0f, 
				angularDrag = 0.1f 
			};
		}

		UseGravityLens = new LensManagerBool(requests => LensUtils.Priority(requests, defaultUseGravity));
		UseGravityLens.OnValueChanged += UseGravityLens_OnValueChanged;

		KinematicLens = new LensManagerBool(requests => LensUtils.AnyTrue(requests, defaultKinematic));
		KinematicLens.OnValueChanged += KinematicLens_OnValueChanged;

		CollisionModeLens = new LensManager<CollisionDetectionMode>(requests => LensUtils.Priority(requests, defaultCollisionMode));
		CollisionModeLens.OnValueChanged += CollisionModeLens_OnValueChanged;

		ConstraintsLens = new LensManager<RigidbodyConstraints>(requests => LensUtils.Priority(requests, defaultConstraints));
		ConstraintsLens.OnValueChanged += ConstraintsLens_OnValueChanged;

		DragLens = new LensManagerFloat(requests => LensUtils.Priority(requests, defaultDrag));
		DragLens.OnValueChanged += DragLens_OnValueChanged;

		AngularDragLens = new LensManagerFloat(requests => LensUtils.Priority(requests, defaultAngularDrag));
		AngularDragLens.OnValueChanged += AngularDragLens_OnValueChanged;

		PropertyItem[] propertyItems = GetComponents<PropertyItem>();
		for (int i = 0; i < propertyItems.Length; i++)
			propertyItems[i].Awake();
	}

	private void OnEnable()
	{
		if (ItemManager.Instance != null)
			ItemManager.Instance.RegisterItem(this);

		OnItemEnabled(this);

		if (collidersDirty)
			NotifyCollidersChanged();
	}

	private void OnDisable()
	{
		if (ItemManager.Instance != null)
			ItemManager.Instance.DeregisterItem(this);

		OnItemDisabled(this);
	}

	public void OnLoaded()
	{
		// OnItemLoaded?
	}

	private void OnValidate()
	{
		/*
		if (!Application.IsPlaying(this) &&
			GetComponent<ItemCollisionSFX>() == null)
		{
			gameObject.AddComponent<ItemCollisionSFX>();
		}
		*/
	}

	public void NotifyCollidersChanged()
	{
		if (isActiveAndEnabled)
		{
			RefreshBounds();
			OnCollidersChanged(this);
			collidersDirty = false;
		}
		else
			collidersDirty = true;
	}

	public void NotifyStateChanged()
	{
		OnStateChanged(this, null);
	}

	private void PropertyItem_OnStateChanged(PropertyItem property)
	{
		OnStateChanged(this, property);
	}


	[ContextMenu("Refresh Colliders")]
	public void RefreshColliders()
	{
		allColliders.Clear();

		Collider[] colliders = GetComponentsInChildren<Collider>(true);
		allColliders.AddRange(colliders);

		NotifyCollidersChanged();
	}

	private void UseGravityLens_OnValueChanged(bool useGravity)
	{
		if(!manualGravity && Rigidbody != null)
			Rigidbody.useGravity = useGravity;
	}

	private void KinematicLens_OnValueChanged(bool isKinematic)
	{
		if (Rigidbody != null)
		{
			Rigidbody.collisionDetectionMode = isKinematic ? CollisionDetectionMode.Discrete : CollisionModeLens;
			Rigidbody.isKinematic = isKinematic;
		}
	}

	private void CollisionModeLens_OnValueChanged(CollisionDetectionMode collisionMode)
	{
		if (Rigidbody != null)
			Rigidbody.collisionDetectionMode = KinematicLens ? CollisionDetectionMode.Discrete : collisionMode;
	}

	private void ConstraintsLens_OnValueChanged(RigidbodyConstraints constraints)
	{
		if (Rigidbody != null)
			Rigidbody.constraints = constraints;
	}

	private void DragLens_OnValueChanged(float drag)
	{
		RigidbodySettings settings = RigidbodySettings;
		settings.drag = drag;
		RigidbodySettings = settings;
	}

	private void AngularDragLens_OnValueChanged(float angularDrag)
	{
		RigidbodySettings settings = RigidbodySettings;
		settings.angularDrag = angularDrag;
		RigidbodySettings = settings;
	}

	public void RemoveRigidbody()
	{
		if (rigidbody == null)
			return;

		OnWillRemoveRigidbody(this, rigidbody);

		backupSettings = RigidbodySettings;

		// Rigidbody only gets destroyed at end of frame
		// so zero velocity / angular velocity incase this happens before FixedUpdate
		// in which case rigidbody will keep on moving

		rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete; // only here to avoid warning
		rigidbody.isKinematic = true;

		rigidbody.velocity = Vector3.zero;
		rigidbody.angularVelocity = Vector3.zero;

		Destroy(rigidbody);

		OnRigidbodyRemoved(this);
	}

	public void RestoreRigidbody()
	{
		rigidbody = gameObject.GetComponent<Rigidbody>();
		if(rigidbody == null)
			rigidbody = gameObject.AddComponent<Rigidbody>();

		rigidbody.useGravity = UseGravityLens;
		rigidbody.isKinematic = KinematicLens;
		rigidbody.constraints = ConstraintsLens;
		rigidbody.collisionDetectionMode = rigidbody.isKinematic ? CollisionDetectionMode.Discrete : CollisionModeLens;

		RigidbodySettings = backupSettings;

		OnRigidbodyAdded(this, rigidbody);
	}

	public void RefreshBounds()
	{
		localBounds = PhysicsUtils.CalculateCollidersBounds(allColliders, transform);
		OnBoundsChanged(this, localBounds);
	}

	public virtual void OnDestroy()
	{
		if (!Singleton.IsQuitting)
		{
			beingDestroyed = true;
			OnItemDestroyed(this);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!Application.IsPlaying(this) && 
			allColliders.Count == 0)
		{
			RefreshColliders();
			RefreshBounds();
		}

		if (Rigidbody != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawLine(transform.position, transform.position + Rigidbody.velocity);

			Gizmos.color = Color.blue;
			Gizmos.DrawLine(transform.position, transform.position + Rigidbody.angularVelocity);
		}

		Gizmos.DrawSphere(Center, 0.02f);

		Matrix4x4 prevMatrix = Gizmos.matrix;

		Gizmos.color = Color.cyan;
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(localBounds.center, localBounds.size);

		Gizmos.matrix = prevMatrix;
	}
}
