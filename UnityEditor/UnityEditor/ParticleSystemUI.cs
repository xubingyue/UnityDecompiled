using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
	internal class ParticleSystemUI
	{
		public enum DefaultTypes
		{
			Root,
			SubBirth,
			SubCollision,
			SubDeath
		}

		protected class Texts
		{
			public GUIContent addModules = new GUIContent("", "Show/Hide Modules");

			public string bulletPoint = "• ";
		}

		public ParticleEffectUI m_ParticleEffectUI;

		public ModuleUI[] m_Modules;

		public ParticleSystem[] m_ParticleSystems;

		public SerializedObject m_ParticleSystemSerializedObject;

		public SerializedObject m_RendererSerializedObject;

		private static string[] s_ModuleNames;

		private string m_SupportsCullingText;

		private string m_SupportsCullingTextLabel;

		private static ParticleSystemUI.Texts s_Texts;

		public bool multiEdit
		{
			get
			{
				return this.m_ParticleSystems != null && this.m_ParticleSystems.Length > 1;
			}
		}

		public void Init(ParticleEffectUI owner, ParticleSystem[] systems)
		{
			if (ParticleSystemUI.s_ModuleNames == null)
			{
				ParticleSystemUI.s_ModuleNames = ParticleSystemUI.GetUIModuleNames();
			}
			if (ParticleSystemUI.s_Texts == null)
			{
				ParticleSystemUI.s_Texts = new ParticleSystemUI.Texts();
			}
			this.m_ParticleEffectUI = owner;
			this.m_ParticleSystems = systems;
			this.m_ParticleSystemSerializedObject = new SerializedObject(this.m_ParticleSystems);
			this.m_RendererSerializedObject = null;
			this.m_SupportsCullingText = null;
			this.m_Modules = ParticleSystemUI.CreateUIModules(this, this.m_ParticleSystemSerializedObject);
			if (!(this.m_ParticleSystems.FirstOrDefault((ParticleSystem o) => o.GetComponent<ParticleSystemRenderer>() == null) != null))
			{
				this.InitRendererUI();
			}
			this.UpdateParticleSystemInfoString();
		}

		internal ModuleUI GetParticleSystemRendererModuleUI()
		{
			return this.m_Modules[this.m_Modules.Length - 1];
		}

		private void InitRendererUI()
		{
			List<ParticleSystemRenderer> list = new List<ParticleSystemRenderer>();
			ParticleSystem[] particleSystems = this.m_ParticleSystems;
			for (int i = 0; i < particleSystems.Length; i++)
			{
				ParticleSystem particleSystem = particleSystems[i];
				ParticleSystemRenderer component = particleSystem.GetComponent<ParticleSystemRenderer>();
				if (component == null)
				{
					particleSystem.gameObject.AddComponent<ParticleSystemRenderer>();
				}
				list.Add(particleSystem.GetComponent<ParticleSystemRenderer>());
			}
			if (list.Count > 0)
			{
				this.m_RendererSerializedObject = new SerializedObject(list.ToArray());
				this.m_Modules[this.m_Modules.Length - 1] = new RendererModuleUI(this, this.m_RendererSerializedObject, ParticleSystemUI.s_ModuleNames[ParticleSystemUI.s_ModuleNames.Length - 1]);
			}
		}

		private void ClearRenderers()
		{
			this.m_RendererSerializedObject = null;
			ParticleSystem[] particleSystems = this.m_ParticleSystems;
			for (int i = 0; i < particleSystems.Length; i++)
			{
				ParticleSystem particleSystem = particleSystems[i];
				ParticleSystemRenderer component = particleSystem.GetComponent<ParticleSystemRenderer>();
				if (component != null)
				{
					Undo.DestroyObjectImmediate(component);
				}
			}
			this.m_Modules[this.m_Modules.Length - 1] = null;
		}

		public float GetEmitterDuration()
		{
			InitialModuleUI initialModuleUI = this.m_Modules[0] as InitialModuleUI;
			float result;
			if (initialModuleUI != null)
			{
				result = initialModuleUI.m_LengthInSec.floatValue;
			}
			else
			{
				result = -1f;
			}
			return result;
		}

		public void OnGUI(float width, bool fixedWidth)
		{
			bool flag = Event.current.type == EventType.Repaint;
			string text = null;
			if (this.m_ParticleSystems.Length > 1)
			{
				text = "Multiple Particle Systems";
			}
			else if (this.m_ParticleSystems.Length > 0)
			{
				text = this.m_ParticleSystems[0].gameObject.name;
			}
			if (fixedWidth)
			{
				EditorGUIUtility.labelWidth = width * 0.4f;
				EditorGUILayout.BeginVertical(new GUILayoutOption[]
				{
					GUILayout.Width(width)
				});
			}
			else
			{
				EditorGUIUtility.labelWidth = 0f;
				EditorGUIUtility.labelWidth -= 4f;
				EditorGUILayout.BeginVertical(new GUILayoutOption[0]);
			}
			InitialModuleUI initial = (InitialModuleUI)this.m_Modules[0];
			for (int i = 0; i < this.m_Modules.Length; i++)
			{
				ModuleUI moduleUI = this.m_Modules[i];
				if (moduleUI != null)
				{
					bool flag2 = moduleUI == this.m_Modules[0];
					if (moduleUI.visibleUI || flag2)
					{
						GUIContent gUIContent = new GUIContent();
						Rect rect;
						if (flag2)
						{
							rect = GUILayoutUtility.GetRect(width, 25f);
						}
						else
						{
							rect = GUILayoutUtility.GetRect(width, 15f);
						}
						if (moduleUI.foldout)
						{
							using (new EditorGUI.DisabledScope(!moduleUI.enabled))
							{
								Rect position = EditorGUILayout.BeginVertical(ParticleSystemStyles.Get().modulePadding, new GUILayoutOption[0]);
								position.y -= 4f;
								position.height += 4f;
								GUI.Label(position, GUIContent.none, ParticleSystemStyles.Get().moduleBgStyle);
								moduleUI.OnInspectorGUI(initial);
								EditorGUILayout.EndVertical();
							}
						}
						if (flag2)
						{
							ParticleSystemRenderer component = this.m_ParticleSystems[0].GetComponent<ParticleSystemRenderer>();
							float num = 21f;
							Rect position2 = new Rect(rect.x + 4f, rect.y + 2f, num, num);
							if (flag && component != null)
							{
								bool flag3 = false;
								int num2 = 0;
								if (!this.multiEdit)
								{
									if (component.renderMode == ParticleSystemRenderMode.Mesh)
									{
										if (component.mesh != null)
										{
											num2 = component.mesh.GetInstanceID();
										}
									}
									else if (component.sharedMaterial != null)
									{
										num2 = component.sharedMaterial.GetInstanceID();
									}
									if (EditorUtility.IsDirty(num2))
									{
										AssetPreview.ClearTemporaryAssetPreviews();
									}
									if (num2 != 0)
									{
										Texture2D assetPreview = AssetPreview.GetAssetPreview(num2);
										if (assetPreview != null)
										{
											GUI.DrawTexture(position2, assetPreview, ScaleMode.StretchToFill, true);
											flag3 = true;
										}
									}
								}
								if (!flag3)
								{
									GUI.Label(position2, GUIContent.none, ParticleSystemStyles.Get().moduleBgStyle);
								}
							}
							if (!this.multiEdit && EditorGUI.DropdownButton(position2, GUIContent.none, FocusType.Passive, GUIStyle.none))
							{
								if (EditorGUI.actionKey)
								{
									List<int> list = new List<int>();
									int instanceID = this.m_ParticleSystems[0].gameObject.GetInstanceID();
									list.AddRange(Selection.instanceIDs);
									if (!list.Contains(instanceID) || list.Count != 1)
									{
										if (list.Contains(instanceID))
										{
											list.Remove(instanceID);
										}
										else
										{
											list.Add(instanceID);
										}
									}
									Selection.instanceIDs = list.ToArray();
								}
								else
								{
									Selection.instanceIDs = new int[0];
									Selection.activeInstanceID = this.m_ParticleSystems[0].gameObject.GetInstanceID();
								}
							}
						}
						Rect position3 = new Rect(rect.x + 2f, rect.y + 1f, 13f, 13f);
						if (!flag2 && GUI.Button(position3, GUIContent.none, GUIStyle.none))
						{
							moduleUI.enabled = !moduleUI.enabled;
						}
						Rect position4 = new Rect(rect.x + rect.width - 10f, rect.y + rect.height - 10f, 10f, 10f);
						Rect position5 = new Rect(position4.x - 4f, position4.y - 4f, position4.width + 4f, position4.height + 4f);
						Rect position6 = new Rect(position4.x - 23f, position4.y - 8f, 20f, 20f);
						if (flag2 && EditorGUI.DropdownButton(position5, ParticleSystemUI.s_Texts.addModules, FocusType.Passive, GUIStyle.none))
						{
							this.ShowAddModuleMenu();
						}
						if (!string.IsNullOrEmpty(text))
						{
							gUIContent.text = ((!flag2) ? moduleUI.displayName : text);
						}
						else
						{
							gUIContent.text = moduleUI.displayName;
						}
						gUIContent.tooltip = moduleUI.toolTip;
						bool flag4 = moduleUI.DrawHeader(rect, gUIContent);
						if (flag4 != moduleUI.foldout)
						{
							int button = Event.current.button;
							if (button != 0)
							{
								if (button == 1)
								{
									if (flag2)
									{
										this.ShowEmitterMenu();
									}
									else
									{
										this.ShowModuleMenu(i);
									}
								}
							}
							else
							{
								bool foldout = !moduleUI.foldout;
								if (Event.current.control)
								{
									ModuleUI[] modules = this.m_Modules;
									for (int j = 0; j < modules.Length; j++)
									{
										ModuleUI moduleUI2 = modules[j];
										if (moduleUI2 != null && moduleUI2.visibleUI)
										{
											moduleUI2.foldout = foldout;
										}
									}
								}
								else
								{
									moduleUI.foldout = foldout;
								}
							}
						}
						if (!flag2)
						{
							EditorGUI.showMixedValue = moduleUI.enabledHasMultipleDifferentValues;
							GUIStyle style = (!EditorGUI.showMixedValue) ? ParticleSystemStyles.Get().checkmark : ParticleSystemStyles.Get().checkmarkMixed;
							GUI.Toggle(position3, moduleUI.enabled, GUIContent.none, style);
							EditorGUI.showMixedValue = false;
						}
						if (flag && flag2)
						{
							GUI.Label(position4, GUIContent.none, ParticleSystemStyles.Get().plus);
						}
						if (flag2 && !string.IsNullOrEmpty(this.m_SupportsCullingTextLabel))
						{
							GUIContent content = new GUIContent("", ParticleSystemStyles.Get().warningIcon, this.m_SupportsCullingTextLabel);
							GUI.Label(position6, content);
						}
						GUILayout.Space(1f);
					}
				}
			}
			GUILayout.Space(-1f);
			EditorGUILayout.EndVertical();
			this.ApplyProperties();
		}

		public void OnSceneViewGUI()
		{
			if (this.m_Modules != null)
			{
				if (ParticleEffectUI.m_ShowBounds)
				{
					ParticleSystem[] particleSystems = this.m_ParticleSystems;
					for (int i = 0; i < particleSystems.Length; i++)
					{
						ParticleSystem ps = particleSystems[i];
						if (this.multiEdit)
						{
							this.ShowBounds(ParticleSystemEditorUtils.GetRoot(ps));
						}
						else
						{
							this.ShowBounds(ps);
						}
					}
				}
				this.UpdateProperties();
				ModuleUI[] modules = this.m_Modules;
				for (int j = 0; j < modules.Length; j++)
				{
					ModuleUI moduleUI = modules[j];
					if (moduleUI != null && moduleUI.visibleUI && moduleUI.enabled)
					{
						if (moduleUI.foldout)
						{
							moduleUI.OnSceneViewGUI();
						}
					}
				}
				this.ApplyProperties();
			}
		}

		private void ShowBounds(ParticleSystem ps)
		{
			if (ps.particleCount > 0)
			{
				ParticleSystemRenderer component = ps.GetComponent<ParticleSystemRenderer>();
				Color color = Handles.color;
				Handles.color = Color.yellow;
				Bounds bounds = component.bounds;
				Handles.DrawWireCube(bounds.center, bounds.size);
				Handles.color = color;
			}
			if (this.multiEdit)
			{
				ParticleSystem[] componentsInChildren = ps.transform.GetComponentsInChildren<ParticleSystem>();
				ParticleSystem[] array = componentsInChildren;
				for (int i = 0; i < array.Length; i++)
				{
					ParticleSystem child = array[i];
					if (child != ps)
					{
						if (!(this.m_ParticleSystems.FirstOrDefault((ParticleSystem o) => ParticleSystemEditorUtils.GetRoot(o) == child) != null))
						{
							this.ShowBounds(child);
						}
					}
				}
			}
		}

		public void ApplyProperties()
		{
			bool hasModifiedProperties = this.m_ParticleSystemSerializedObject.hasModifiedProperties;
			if (this.m_ParticleSystemSerializedObject.targetObject != null)
			{
				this.m_ParticleSystemSerializedObject.ApplyModifiedProperties();
			}
			if (hasModifiedProperties)
			{
				ParticleSystem[] particleSystems = this.m_ParticleSystems;
				for (int i = 0; i < particleSystems.Length; i++)
				{
					ParticleSystem ps = particleSystems[i];
					ParticleSystem root = ParticleSystemEditorUtils.GetRoot(ps);
					if (!ParticleEffectUI.IsStopped(root) && ParticleSystemEditorUtils.resimulation)
					{
						ParticleSystemEditorUtils.PerformCompleteResimulation();
					}
				}
				this.UpdateParticleSystemInfoString();
			}
			if (this.m_RendererSerializedObject != null && this.m_RendererSerializedObject.targetObject != null)
			{
				this.m_RendererSerializedObject.ApplyModifiedProperties();
			}
		}

		private void UpdateParticleSystemInfoString()
		{
			string text = "";
			ModuleUI[] modules = this.m_Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				ModuleUI moduleUI = modules[i];
				if (moduleUI != null && moduleUI.visibleUI && moduleUI.enabled)
				{
					moduleUI.UpdateCullingSupportedString(ref text);
				}
			}
			if (text != string.Empty)
			{
				if (text != this.m_SupportsCullingText || this.m_SupportsCullingTextLabel == null)
				{
					this.m_SupportsCullingText = text;
					this.m_SupportsCullingTextLabel = "Automatic culling is disabled because: " + text.Replace("\n", "\n" + ParticleSystemUI.s_Texts.bulletPoint);
				}
			}
			else
			{
				this.m_SupportsCullingText = null;
				this.m_SupportsCullingTextLabel = null;
			}
		}

		public void UpdateProperties()
		{
			if (this.m_ParticleSystemSerializedObject.targetObject != null)
			{
				this.m_ParticleSystemSerializedObject.UpdateIfRequiredOrScript();
			}
			if (this.m_RendererSerializedObject != null && this.m_RendererSerializedObject.targetObject != null)
			{
				this.m_RendererSerializedObject.UpdateIfRequiredOrScript();
			}
		}

		private void ResetModules()
		{
			ModuleUI[] modules = this.m_Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				ModuleUI moduleUI = modules[i];
				if (moduleUI != null)
				{
					moduleUI.enabled = false;
					if (!ParticleEffectUI.GetAllModulesVisible())
					{
						moduleUI.visibleUI = false;
					}
				}
			}
			if (this.m_Modules[this.m_Modules.Length - 1] == null)
			{
				this.InitRendererUI();
			}
			int[] array = new int[]
			{
				1,
				2,
				this.m_Modules.Length - 1
			};
			for (int j = 0; j < array.Length; j++)
			{
				int num = array[j];
				if (this.m_Modules[num] != null)
				{
					this.m_Modules[num].enabled = true;
					this.m_Modules[num].visibleUI = true;
				}
			}
		}

		private void ShowAddModuleMenu()
		{
			GenericMenu genericMenu = new GenericMenu();
			for (int i = 0; i < ParticleSystemUI.s_ModuleNames.Length; i++)
			{
				if (this.m_Modules[i] == null || !this.m_Modules[i].visibleUI)
				{
					genericMenu.AddItem(new GUIContent(ParticleSystemUI.s_ModuleNames[i]), false, new GenericMenu.MenuFunction2(this.AddModuleCallback), i);
				}
				else
				{
					genericMenu.AddDisabledItem(new GUIContent(ParticleSystemUI.s_ModuleNames[i]));
				}
			}
			genericMenu.AddSeparator("");
			genericMenu.AddItem(EditorGUIUtility.TrTextContent("Show All Modules", null, null), ParticleEffectUI.GetAllModulesVisible(), new GenericMenu.MenuFunction2(this.AddModuleCallback), 10000);
			genericMenu.ShowAsContext();
			Event.current.Use();
		}

		private void AddModuleCallback(object obj)
		{
			int num = (int)obj;
			if (num >= 0 && num < this.m_Modules.Length)
			{
				if (num == this.m_Modules.Length - 1)
				{
					this.InitRendererUI();
				}
				else
				{
					this.m_Modules[num].enabled = true;
					this.m_Modules[num].foldout = true;
				}
			}
			else
			{
				this.m_ParticleEffectUI.SetAllModulesVisible(!ParticleEffectUI.GetAllModulesVisible());
			}
			this.ApplyProperties();
		}

		private void ModuleMenuCallback(object obj)
		{
			int num = (int)obj;
			bool flag = num == this.m_Modules.Length - 1;
			if (flag)
			{
				this.ClearRenderers();
			}
			else
			{
				if (!ParticleEffectUI.GetAllModulesVisible())
				{
					this.m_Modules[num].visibleUI = false;
				}
				this.m_Modules[num].enabled = false;
			}
		}

		private void ShowModuleMenu(int moduleIndex)
		{
			GenericMenu genericMenu = new GenericMenu();
			if (!ParticleEffectUI.GetAllModulesVisible())
			{
				genericMenu.AddItem(EditorGUIUtility.TrTextContent("Remove", null, null), false, new GenericMenu.MenuFunction2(this.ModuleMenuCallback), moduleIndex);
			}
			else
			{
				genericMenu.AddDisabledItem(EditorGUIUtility.TrTextContent("Remove", null, null));
			}
			genericMenu.ShowAsContext();
			Event.current.Use();
		}

		private void EmitterMenuCallback(object obj)
		{
			switch ((int)obj)
			{
			case 0:
				this.m_ParticleEffectUI.CreateParticleSystem(this.m_ParticleSystems[0], SubModuleUI.SubEmitterType.None);
				break;
			case 1:
				this.ResetModules();
				break;
			case 2:
				EditorGUIUtility.PingObject(this.m_ParticleSystems[0]);
				break;
			}
		}

		private void ShowEmitterMenu()
		{
			GenericMenu genericMenu = new GenericMenu();
			genericMenu.AddItem(EditorGUIUtility.TrTextContent("Show Location", null, null), false, new GenericMenu.MenuFunction2(this.EmitterMenuCallback), 2);
			genericMenu.AddSeparator("");
			if (this.m_ParticleSystems[0].gameObject.activeInHierarchy)
			{
				genericMenu.AddItem(EditorGUIUtility.TrTextContent("Create Particle System", null, null), false, new GenericMenu.MenuFunction2(this.EmitterMenuCallback), 0);
			}
			else
			{
				genericMenu.AddDisabledItem(EditorGUIUtility.TrTextContent("Create new Particle System", null, null));
			}
			genericMenu.AddItem(EditorGUIUtility.TrTextContent("Reset", null, null), false, new GenericMenu.MenuFunction2(this.EmitterMenuCallback), 1);
			genericMenu.ShowAsContext();
			Event.current.Use();
		}

		private static ModuleUI[] CreateUIModules(ParticleSystemUI e, SerializedObject so)
		{
			int num = 0;
			ModuleUI[] expr_0A = new ModuleUI[23];
			expr_0A[0] = new InitialModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[1] = new EmissionModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[2] = new ShapeModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[3] = new VelocityModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[4] = new ClampVelocityModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[5] = new InheritVelocityModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[6] = new ForceModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[7] = new ColorModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[8] = new ColorByVelocityModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[9] = new SizeModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[10] = new SizeByVelocityModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[11] = new RotationModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[12] = new RotationByVelocityModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[13] = new ExternalForcesModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[14] = new NoiseModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[15] = new CollisionModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[16] = new TriggerModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[17] = new SubModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[18] = new UVModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[19] = new LightsModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[20] = new TrailModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			expr_0A[21] = new CustomDataModuleUI(e, so, ParticleSystemUI.s_ModuleNames[num++]);
			return expr_0A;
		}

		public static string[] GetUIModuleNames()
		{
			return new string[]
			{
				"",
				L10n.Tr("Emission"),
				L10n.Tr("Shape"),
				L10n.Tr("Velocity over Lifetime"),
				L10n.Tr("Limit Velocity over Lifetime"),
				L10n.Tr("Inherit Velocity"),
				L10n.Tr("Force over Lifetime"),
				L10n.Tr("Color over Lifetime"),
				L10n.Tr("Color by Speed"),
				L10n.Tr("Size over Lifetime"),
				L10n.Tr("Size by Speed"),
				L10n.Tr("Rotation over Lifetime"),
				L10n.Tr("Rotation by Speed"),
				L10n.Tr("External Forces"),
				L10n.Tr("Noise"),
				L10n.Tr("Collision"),
				L10n.Tr("Triggers"),
				L10n.Tr("Sub Emitters"),
				L10n.Tr("Texture Sheet Animation"),
				L10n.Tr("Lights"),
				L10n.Tr("Trails"),
				L10n.Tr("Custom Data"),
				L10n.Tr("Renderer")
			};
		}
	}
}
