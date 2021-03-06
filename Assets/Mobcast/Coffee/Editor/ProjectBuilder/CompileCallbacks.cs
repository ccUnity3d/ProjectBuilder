﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Mobcast.Coffee.Build
{
	/// <summary>
	/// Project builder post process.
	/// </summary>
	internal class CompileCallbacks : ScriptableSingleton<CompileCallbacks>
	{
		/// <summary>On finished compile callback.</summary>
		[SerializeField] List<string> m_OnFinishedCompile = new List<string>();

		static bool s_IsCompiling = false;

		/// <summary>メソッドリストを全てコールします.</summary>
		void OnFinishedCompile(bool successfully)
		{
			foreach (var methodPath in m_OnFinishedCompile.ToArray())
			{
				try
				{
					string className = Path.GetFileNameWithoutExtension(methodPath);
					string methodName = Path.GetExtension(methodPath).TrimStart('.');
					MethodInfo ret = Type.GetType(className).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
					ret.Invoke(null, new object[]{ successfully });
				}
				catch (Exception e)
				{
					Debug.LogError(methodPath + " cannnot call. " + e.Message);
				}
				m_OnFinishedCompile.Remove(methodPath);
			}
		}

		/// <summary>
		/// Called on finished compile.
		/// Supports static method only.
		/// </summary>
		public static event Action<bool> onFinishedCompile
		{
			add
			{
				string path = string.Format("{0}.{1}", value.Method.DeclaringType.FullName, value.Method.Name);
				if (instance.m_OnFinishedCompile.Contains(path))
					Debug.LogError(path + " already be registered.");
				else if (!value.Method.IsStatic)
					Debug.LogError(path + " is not static method.");
				else if (value.Method.Name.StartsWith("<"))
					Debug.LogError(path + " is anonymous method.");
				else
					instance.m_OnFinishedCompile.Add(path);
			}
			remove
			{
				instance.m_OnFinishedCompile.Remove(string.Format("{0}.{1}", value.Method.DeclaringType.FullName, value.Method.Name));
			}
		}

		/// <summary>
		/// On finished compile successfully.
		/// </summary>
		[InitializeOnLoadMethod]
		static void OnFinishedCompileSuccessfully()
		{
			// Compiling is finished successfully.
			// Call OnFinishedCompile callback next frame.
			EditorApplication.delayCall += () =>
			{
				instance.OnFinishedCompile(true);

				// Observe compile error until next compile.
				EditorApplication.update += () =>
				{
					if (s_IsCompiling == EditorApplication.isCompiling)
						return;

					s_IsCompiling = EditorApplication.isCompiling;
//					Debug.Log("changed compile status " + s_IsCompiling);

					// Compile has stopped with errors.
					if (!s_IsCompiling)
						instance.OnFinishedCompile(false);
				};
			};
		}
	}
}