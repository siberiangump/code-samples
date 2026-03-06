using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;

[ExecuteInEditMode]
public class AutoFillReference : MonoBehaviour
{
	[SerializeField, ValidateField(typeof(IContext))] MonoBehaviour Scene;
	[SerializeField, ValidateField(typeof(IContext))] MonoBehaviour Asset;

	private Scene CurrentScene;

	[SerializeField] AssemblyData[] Assembly;
	[SerializeField] TargetData[] TargetsList;
	[SerializeField] SerializationData[] SerializationList;
	[SerializeField] FieldData[] ReferenceList;

	[Header("Issues")]
	[SerializeField] FieldData[] ReferenceWithoutListener;
	[SerializeField] FieldData[] SerializationWithoutReference;

	private void OnValidate()
	{
		DoAll();
	}

#if UNITY_EDITOR

	private void OnEnable()
	{
		EditorSceneManager.sceneSaving -= OnSceneSaving;
		EditorSceneManager.sceneSaving += OnSceneSaving;
	}

	private void OnSceneSaving(Scene scene, string path)
	{
		if (this?.gameObject == null)
		{
			EditorSceneManager.sceneSaving -= OnSceneSaving;
			Debug.LogError("[AutoFillReference] Remove");
			return;
		}
		DoAll();
		Debug.Log("[AutoFillReference] AllGood");
	}

#endif

#region Validate

	[ContextMenu("Validate")]
	public void Validate()
	{
		ReferenceWithoutListener = ValidateReferences(ReferenceList, SerializationList);
		SerializationWithoutReference = ValidateSerializations(ReferenceList, SerializationList);
		ValidateTargets(TargetsList);
	}

	private void ValidateTargets(TargetData[] targetsList)
	{
		foreach (TargetData item in targetsList)
		{
			object value = item.Field.GetFieldValue(item.Target);
			if (value == null)
				Debug.LogError(item.Id, item.Target.gameObject);
		}
	}

	private FieldData[] ValidateReferences(FieldData[] references, SerializationData[] serializations)
	{
		List<FieldData> refs = new List<FieldData>();
		foreach (var item in references)
		{
			SerializationData serialization = serializations.FirstOrDefault(x => x.Field.FieldType == item.FieldType);
			if (serialization.Id == null && serialization.Field.Context == item.Context	&& serialization.Field.Marker == item.Marker)
				refs.Add(item);
		}
		return refs.ToArray();
	}

	private FieldData[] ValidateSerializations(FieldData[] references, SerializationData[] serializations)
	{
		List<FieldData> serials = new List<FieldData>();
		foreach (var item in serializations)
		{
			FieldData reference = references.FirstOrDefault(x => x.FieldType == item.Field.FieldType);
			if (reference.FieldName == null)
			{
				FieldData data = FieldData.FromSerialization(item);
				serials.Add(data);
				string mark = data.Marker == Marker.None ? "" : $",Mark(Marker.{data.Marker})";
				MonoBehaviour context = data.Context == Context.Asset ? Asset as MonoBehaviour : Scene as MonoBehaviour;
				Debug.LogError($"please add \n [SerializeField{mark}] internal {data.FieldType} {data.FieldName}; \n to context", context);
			}
		}
		return serials.ToArray();
	}

#endregion

#region AutoFill

	[ContextMenu("DoAll")]
	public void DoAll()
	{
		AssemblyUpdate();
		UpdateMapping();
		DoAutoFill();
		Validate();
	}

	[ContextMenu("DoAutoFill")]
	public void DoAutoFill()
	{
		AutoFill(ReferenceList, TargetsList);
	}

	public void AutoFill(FieldData[] references, TargetData[] targets)
	{
		foreach (var item in references)
		{
			MonoBehaviour context = item.Context == Context.Asset ? Asset as MonoBehaviour : Scene as MonoBehaviour;
			object value = item.GetFieldValue(context);
			if (value == null)
			{
				Debug.LogError($"{item.Context} context have not filled reference on {item.FieldName}", context);
				continue;
			}
			foreach (var target in targets.Where(x => x.Field.IsFieldMatch(item)))
			{
				FieldInfo fieldInfo = target.Field.GetFieldInfo(target.Target);
				fieldInfo.SetValue(target.Target, value);
#if UNITY_EDITOR
				EditorUtility.SetDirty(target.Target);
#endif
			}
		}
	}

	public void AutoFillTarget(GameObject gameObject)
	{
		TargetData[] targetsData = GetTargets(new GameObject[1] { gameObject }, Assembly);
		AutoFill(ReferenceList, targetsData);
		ValidateTargets(targetsData);
	}

#endregion

#region AssemblyUpdate

	[ContextMenu("AssemblyUpdate")]
	public void AssemblyUpdate()
	{
		Assembly = GetAllTypeWithAutoFill();
	}

	public AssemblyData[] GetAllTypeWithAutoFill()
	{
		List<AssemblyData> typeMemberPairs = new List<AssemblyData>();

		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (var item in assemblies)
		{
			Type[] types = item.GetTypes();
			foreach (var type in types)
			{
				FieldInfo[] memberInfos = type.GetAllMemberWithAttribute<AutoFill>();
				List<FieldData> memberData = new List<FieldData>();
				foreach (var member in memberInfos)
					memberData.Add(FieldData.FromFildInfo(member));
				if (memberData.Count > 0)
					typeMemberPairs.Add(AssemblyData.FromType(type, memberData.ToArray()));
			}
		}
		return typeMemberPairs.ToArray();
	}

#endregion

#region UpdateMapping

	[ContextMenu("Update")]
	public void UpdateMapping()
	{
		CurrentScene = this.gameObject.scene;
		if(CurrentScene.isLoaded == false) 
			return;

		TargetsList = GetTargets(CurrentScene.GetRootGameObjects(), Assembly);
		SerializationList = GetSerialization(TargetsList);
		ReferenceList = GetReferences();
	}

	public TargetData[] GetTargets(GameObject[] roots, AssemblyData[] assemblyes)
	{
		List<TargetData> targets = new List<TargetData>();
		for (int i = 0; i < roots.Length; i++)
		{
			foreach (var item in assemblyes)
			{
				List<Component> components = new List<Component>();
				components.AddRange(roots[i].GetComponents(item.Type));
				components.AddRange(roots[i].GetComponentsInChildren(item.Type));
				foreach (var component in components)
				{
					foreach (var member in item.MembersData)
					{
						targets.Add(TargetData.FromFieldData(member, component as MonoBehaviour));
					}
				}
			}
		}
		return targets.ToArray();
	}

	private SerializationData[] GetSerialization(TargetData[] tergets)
	{
		List<SerializationData> serializations = new List<SerializationData>();
		List<string> ids = new List<string>();
		foreach (var item in tergets)
		{
			string type = item.Field.FieldName;
			string id = SerializationData.GetId(type, item.Field.Marker);
			if (ids.Contains(id))
				continue;
			ids.Add(id);
			serializations.Add(SerializationData.FromTargetData(item));
		}
		return serializations.ToArray();
	}

	private FieldData[] GetReferences()
	{
		List<FieldData> references = new List<FieldData>();
		references.AddRange((Scene as IContext).GetFields());
		references.AddRange((Asset as IContext).GetFields());
		return references.ToArray();
	}

#endregion

}

[System.Serializable]
public struct AssemblyData
{
	internal Type Type;
	[SerializeField] internal string TypeName;

	[SerializeField] internal FieldData[] MembersData;

	public static AssemblyData FromType(Type type, FieldData[] members)
	{
		return new AssemblyData
		{
			Type = type,
			TypeName = type.Name,
			MembersData = members
		};
	}
}

[System.Serializable]
public struct FieldData
{
	[SerializeField] internal string FieldName;
	[SerializeField] internal string FieldType;
	[SerializeField] internal Context Context;
	[SerializeField] internal Marker Marker;

	public static FieldData FromFildInfo(FieldInfo field)
	{
		AutoFill att = field.GetCustomAttribute<AutoFill>();
		return new FieldData
		{
			FieldName = field.Name,
			Context = att.Context,
			Marker = att.Marker,
			FieldType = field.FieldType.Name,
		};
	}

	public static FieldData FromSerialization(SerializationData serialization)
	{
		return new FieldData
		{
			FieldName = serialization.Field.FieldName,
			Context = serialization.Field.Context,
			Marker = serialization.Field.Marker,
			FieldType = serialization.Field.FieldType
		};
	}
}

public static class FieldDataExtensions
{
	public static bool IsFieldMatch(this FieldData one, FieldData two)
	{
		return one.Context == two.Context && one.Marker == two.Marker && one.FieldType == two.FieldType;
	}

	public static object GetFieldValue(this FieldData fields, MonoBehaviour target)
	{
		Type type = target.GetType();
		BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
		FieldInfo fieldInfo = null;
		while (fieldInfo == null)
		{
			fieldInfo = type.GetField(fields.FieldName, bindingFlags);
			type = type.BaseType;
		}
		return fieldInfo.GetValue(target);
	}

	public static FieldInfo GetFieldInfo(this FieldData data, MonoBehaviour mono)
	{
		Type type = mono.GetType();
		BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		FieldInfo fieldInfo = null;
		while (fieldInfo == null)
		{
			fieldInfo = type.GetField(data.FieldName, bindingFlags);
			type = type.BaseType;
		}
		return fieldInfo;
	}
}

[System.Serializable]
public struct TargetData
{
	[SerializeField] internal string Id;
	[SerializeField] internal MonoBehaviour Target;
	[SerializeField] internal FieldData Field;

	internal static string GetId(FieldData field, MonoBehaviour target)
	{
		return $"{target.name}.{field.FieldName}({field.FieldType})";
	}

	internal static TargetData FromFieldData(FieldData field, MonoBehaviour target)
	{
		return new TargetData
		{
			Id = TargetData.GetId(field, target),
			Target = target,
			Field = field
		};
	}
}

[System.Serializable]
public struct SerializationData
{
	[SerializeField] internal string Id;
	[SerializeField] internal FieldData Field;

	internal static string GetId(string typeName, Marker marker)
	{
		string mark = marker == Marker.None ? string.Empty : marker.ToString();
		return $"{mark}{typeName}";
	}

	internal static SerializationData FromTargetData(TargetData target)
	{
		string type = target.Field.FieldType;
		string id = GetId(type, target.Field.Marker);
		return new SerializationData { Id = id, Field = target.Field };
	}
}

[System.Serializable]
internal struct ContextText
{
	[SerializeField] internal string ClassHeader;
	[SerializeField] internal string Serialization;
	[SerializeField] internal string ClassEnd;
}

[AttributeUsage(AttributeTargets.Field)]
public class AutoFill : Attribute
{
	public Context Context;
	public Marker Marker;

	public AutoFill(Context context, Marker marker = Marker.None)
	{
		Context = context;
		Marker = marker;
	}
}

[AttributeUsage(AttributeTargets.Field)]
public class Mark : Attribute
{
	public Marker Marker;

	public Mark(Marker marker)
	{
		Marker = marker;
	}
}

public interface IContext
{
	Context GetContextType();
}

public static class IContextExtensions
{
	public static FieldData[] GetFields(this IContext context)
	{
		List<FieldData> references = new List<FieldData>();

		Type type = context.GetType();
		Context contextType = context.GetContextType();

		references.AddRange(GetFields(type));

		return references.ToArray();

		List<FieldData> GetFields(Type targetType)
		{
			List<FieldData> refs = new List<FieldData>();
			FieldInfo[] fields = targetType.GetAllMemberWithAttribute<SerializeField>();

			foreach (var item in fields)
			{
				Mark mark = item.GetCustomAttribute<Mark>();
				Marker marker = mark != null ? mark.Marker : Marker.None;
				refs.Add(new FieldData { Context = contextType, FieldType = item.FieldType.Name, FieldName = item.Name, Marker = marker });
			}
			return refs;
		}
	}
}

public static class ReflectionExtantions
{
	public static FieldInfo[] GetAllMemberWithAttribute<T>(this Type type) where T : Attribute
	{
		BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		FieldInfo[] fieldsInfo = type.GetFieldsIncludeBase();
		List<FieldInfo> members = new List<FieldInfo>();
		foreach (var member in fieldsInfo)
		{
			if (member.GetCustomAttributes().ToArray().Length <= 0)
				continue;
			T att = member.GetCustomAttribute<T>();
			if (att == null)
				continue;
			members.Add(member);
		}
		return members.ToArray();
	}

	public static FieldInfo[] GetFieldsIncludeBase(this Type type)
	{
		BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		List<FieldInfo> fieldsInfo = new List<FieldInfo>();
		while (type != null) 
		{
			fieldsInfo.AddRange(type.GetFields(bindingFlags));
			type = type.BaseType;
		}
		return fieldsInfo.ToArray();
	}
}

public enum Context { Scene, Asset }