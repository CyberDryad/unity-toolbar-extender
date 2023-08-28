#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

/// <summary>
/// Adds an extension to the unity toolbar in order to play from a desired entry scene. As well as quickly open specified scenes in unity from toolbar buttons.
/// See below important notes for setup within projects.
/// </summary>


static class ToolbarStyles
{
	public static readonly GUIStyle commandButtonStyle;

	static ToolbarStyles()
	{
		commandButtonStyle = new GUIStyle("Command")
		{
			fontSize = 16,
			alignment = TextAnchor.MiddleCenter,
			imagePosition = ImagePosition.ImageAbove,
			fontStyle = FontStyle.Bold
		};
	}
}

[InitializeOnLoad]
public class ToolbarExtensions
{
	/// IMPORTANT
	/// Change these constants to the desired scene to launch and description when setting up in new projects
	internal const string SCENE_TO_LAUNCH_FROM = "Scene1";
	internal const string LAUNCH_BUTTON_DESCRIPTION = "Play Scene1";

	internal const string SCENE_1 = "Scene1";
	internal const string SCENE_1_DESCRIPTION = "Play Scene1";

	private static string CachedOpenScene
    {
        get
        {
            return EditorPrefs.GetString("toolbar_extensions_cached_open_scene");
        }
        set
        {
            EditorPrefs.SetString("toolbar_extensions_cached_open_scene", value);
        }
    }

	static ToolbarExtensions()
    {
        ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUILeft);
        ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUIRight);
    }

	static void OnToolbarGUILeft()
	{
		GUILayout.FlexibleSpace();

		if(GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("StepButton").image, LAUNCH_BUTTON_DESCRIPTION), ToolbarStyles.commandButtonStyle))
		{
			PlayFromPrelaunchScene();
		}
		if(GUILayout.Button(new GUIContent("1", SCENE_1_DESCRIPTION), ToolbarStyles.commandButtonStyle))
		{
			SceneHelper.StartScene(SCENE_1);
		}
	}

    static void OnToolbarGUIRight()
	{
		
	}

	public static void PlayFromPrelaunchScene()
    {
        if ( EditorApplication.isPlaying == true )
        {
            EditorApplication.playModeStateChanged += RestoreOpenScene;
            EditorApplication.isPlaying = false;
            return;
        }
        CachedOpenScene = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

		//IMPORTANT: can get scene to open by index if prefered
        // EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(0));
		//IMPORTANT: can get scene by constant if prefered
		string[] guids = AssetDatabase.FindAssets("t:scene " + SCENE_TO_LAUNCH_FROM, null);
		if (guids.Length == 0)
		{
			Debug.LogWarning("Couldn't find scene file");
		}
		else
		{
			string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
			EditorSceneManager.OpenScene(scenePath);
		}

        EditorApplication.isPlaying = true;
    }

    private static void RestoreOpenScene(PlayModeStateChange change)
    {
        if(change == PlayModeStateChange.EnteredEditMode)
        {
            if(!string.IsNullOrEmpty(CachedOpenScene))
                EditorSceneManager.OpenScene(CachedOpenScene);

            EditorApplication.playModeStateChanged -= RestoreOpenScene;
        }
    }

	static class SceneHelper
	{
		static string sceneToOpen;

		public static void StartScene(string sceneName)
		{
			if(EditorApplication.isPlaying)
			{
				EditorApplication.isPlaying = false;
			}

			sceneToOpen = sceneName;
			EditorApplication.update += OnUpdate;
		}

		static void OnUpdate()
		{
			if (sceneToOpen == null ||
			    EditorApplication.isPlaying || EditorApplication.isPaused ||
			    EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}

			EditorApplication.update -= OnUpdate;

			if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				// need to get scene via search because the path to the scene
				// file contains the package version so it'll change over time
				string[] guids = AssetDatabase.FindAssets("t:scene " + sceneToOpen, null);
				if (guids.Length == 0)
				{
					Debug.LogWarning("Couldn't find scene file");
				}
				else
				{
					string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
					EditorSceneManager.OpenScene(scenePath);
					EditorApplication.isPlaying = true;
				}
			}
			sceneToOpen = null;
		}
	}
	
	//Used from Unity Toolbar Extender Package Example https://github.com/marijnz/unity-toolbar-extender/blob/master/Example~/SceneSwitcher/Editor/SceneSwitcher.cs
	[InitializeOnLoad]
	public static class SceneViewFocuser
	{
		static bool m_enabled;

		static bool Enabled
		{
			get { return m_enabled; }
			set
			{
				m_enabled = value;
				EditorPrefs.SetBool("SceneViewFocuser", value);
			}
		}

		static SceneViewFocuser()
		{
			m_enabled = EditorPrefs.GetBool("SceneViewFocuser", false);
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
			EditorApplication.pauseStateChanged += OnPauseChanged;

			ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
		}

		static void OnPauseChanged(PauseState obj)
		{
			if (Enabled && obj == PauseState.Unpaused)
			{
				// Not sure why, but this must be delayed
				EditorApplication.delayCall += EditorWindow.FocusWindowIfItsOpen<SceneView>;
			}
		}

		static void OnPlayModeChanged(PlayModeStateChange obj)
		{
			if (Enabled && obj == PlayModeStateChange.EnteredPlayMode)
			{
				EditorWindow.FocusWindowIfItsOpen<SceneView>();
			}
		}

		static void OnToolbarGUI()
		{
			var tex = EditorGUIUtility.IconContent(@"UnityEditor.SceneView").image;

			GUI.changed = false;

			GUILayout.Toggle(m_enabled, new GUIContent(null, tex, "Focus SceneView when entering play mode"), "Command");
			if (GUI.changed)
			{
				Enabled = !Enabled;
			}
		}
	}
}
#endif