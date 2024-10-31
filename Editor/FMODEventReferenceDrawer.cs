using UnityEditor;
using System.Linq; 
using System.Collections.Generic;

#if FMOD
using FMODUnity;
using FMOD;  

public class FMODEventReferenceDrawer : PropertyDrawer
{
	public string[] GetEventParamaters(EventReference reference)
	{

		EditorEventRef eventRef = EventManager.EventFromPath(reference.Path);

		int paramCount = eventRef.LocalParameters.Count(); 

		List<string> paramaterNames = new List<string>();

		if (paramCount > 0)
		{
			for (int i = 0; i < paramCount; i++)
			{
				paramaterNames.Add(eventRef.LocalParameters[i].name);
			}
		}

		return paramaterNames.ToArray();
	}
}

#endif