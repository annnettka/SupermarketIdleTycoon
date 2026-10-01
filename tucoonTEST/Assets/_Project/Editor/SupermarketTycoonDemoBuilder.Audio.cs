using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace SupermarketTycoon.Editor
{
    /// <summary>
    /// Owns the audio-import and generated-mixer portion of the playable-demo builder.
    /// Владеет частью конструктора демо, отвечающей за импорт аудио и создаваемый микшер.
    /// </summary>
    public static partial class SupermarketTycoonDemoBuilder
    {
        private const string AudioRoot = Root + "/Audio";
        private const string AudioMixerPath = AudioRoot + "/SupermarketAudio.mixer";
        private const string MenuMusicPath = Root + "/vintage_menu.mp3";
        private const string GameplayMusicPath = Root + "/Two Left Socks.mp3";

        private static void EnsureAudioAssets()
        {
            ConfigureMusicImporter(MenuMusicPath);
            ConfigureMusicImporter(GameplayMusicPath);
            EnsureAudioMixer();
        }

        private static AudioMixer EnsureAudioMixer()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioMixerPath);
            var controllerType = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
            var groupType = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerGroupController");
            var parameterPathType = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");
            if (controllerType == null || groupType == null || parameterPathType == null)
            {
                Debug.LogError("Unity audio mixer editor APIs are unavailable; the project mixer could not be generated.");
                return mixer;
            }

            object controller = mixer;
            if (controller == null)
            {
                var create = controllerType.GetMethod(
                    "CreateMixerControllerAtPath",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                controller = create?.Invoke(null, new object[] { AudioMixerPath });
                mixer = controller as AudioMixer;
            }

            if (controller == null || mixer == null)
            {
                Debug.LogError($"Could not create audio mixer at '{AudioMixerPath}'.");
                return null;
            }

            var master = controllerType.GetProperty(
                "masterGroup",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(controller);
            if (master == null)
            {
                Debug.LogError("Generated audio mixer has no Master group.");
                return mixer;
            }

            var music = EnsureMixerGroup(controller, master, "Music", controllerType);
            var sfx = EnsureMixerGroup(controller, master, "SFX", controllerType);
            EnsureExposedVolume(controller, master, "MasterVolume", controllerType, groupType, parameterPathType);
            EnsureExposedVolume(controller, music, "MusicVolume", controllerType, groupType, parameterPathType);
            EnsureExposedVolume(controller, sfx, "SfxVolume", controllerType, groupType, parameterPathType);

            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            ValidateAudioMixer(mixer, controller, controllerType);
            return mixer;
        }

        private static void ValidateAudioMixer(AudioMixer mixer, object controller, Type controllerType)
        {
            if (mixer == null ||
                FindMixerGroup(mixer, "Music") == null ||
                FindMixerGroup(mixer, "SFX") == null)
            {
                throw new InvalidOperationException("The generated audio mixer is missing its Music or SFX group.");
            }

            var exposedProperty = controllerType.GetProperty(
                "exposedParameters",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (exposedProperty?.GetValue(controller) is not Array parameters)
            {
                throw new InvalidOperationException("The generated audio mixer volume parameters are not exposed correctly.");
            }

            var names = new System.Collections.Generic.HashSet<string>();
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters.GetValue(i);
                var name = parameter?.GetType().GetField(
                    "name",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(parameter) as string;
                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
                }
            }

            if (!names.Contains("MasterVolume") ||
                !names.Contains("MusicVolume") ||
                !names.Contains("SfxVolume"))
            {
                throw new InvalidOperationException("The generated audio mixer volume parameters are not exposed correctly.");
            }
        }

        private static object EnsureMixerGroup(
            object controller,
            object master,
            string groupName,
            Type controllerType)
        {
            var getAllGroups = controllerType.GetMethod(
                "GetAllAudioGroupsSlow",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (getAllGroups?.Invoke(controller, null) is IEnumerable groups)
            {
                foreach (var group in groups)
                {
                    if (group is UnityEngine.Object unityObject && unityObject.name == groupName)
                    {
                        return group;
                    }
                }
            }

            var createGroup = controllerType.GetMethod(
                "CreateNewGroup",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var child = createGroup?.Invoke(controller, new object[] { groupName, false });
            var addChild = controllerType.GetMethod(
                "AddChildToParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            addChild?.Invoke(controller, new[] { child, master });
            return child;
        }

        private static void EnsureExposedVolume(
            object controller,
            object group,
            string parameterName,
            Type controllerType,
            Type groupType,
            Type parameterPathType)
        {
            if (group == null)
            {
                return;
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var guid = groupType.GetMethod("GetGUIDForVolume", flags)?.Invoke(group, null);
            if (guid == null)
            {
                return;
            }

            var exposedProperty = controllerType.GetProperty("exposedParameters", flags);
            if (TryRenameExposedParameter(exposedProperty, controller, guid, parameterName))
            {
                return;
            }

            var constructor = parameterPathType.GetConstructor(
                flags,
                null,
                new[] { groupType, guid.GetType() },
                null);
            var path = constructor?.Invoke(new[] { group, guid });
            if (path == null)
            {
                Debug.LogError($"Could not expose the {parameterName} mixer parameter.");
                return;
            }

            controllerType.GetMethod("AddExposedParameter", flags)?.Invoke(controller, new[] { path });
            TryRenameExposedParameter(exposedProperty, controller, guid, parameterName);
        }

        private static bool TryRenameExposedParameter(
            PropertyInfo exposedProperty,
            object controller,
            object guid,
            string parameterName)
        {
            if (exposedProperty?.GetValue(controller) is not Array parameters)
            {
                return false;
            }

            var found = false;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters.GetValue(i);
                if (parameter == null)
                {
                    continue;
                }

                var fields = parameter.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var guidField = Array.Find(fields, field => field.Name == "guid");
                var nameField = Array.Find(fields, field => field.Name == "name");
                if (guidField?.GetValue(parameter)?.Equals(guid) != true)
                {
                    continue;
                }

                nameField?.SetValue(parameter, parameterName);
                parameters.SetValue(parameter, i);
                found = true;
                break;
            }

            if (found)
            {
                exposedProperty.SetValue(controller, parameters);
            }

            return found;
        }

        private static AudioMixerGroup FindMixerGroup(AudioMixer mixer, string groupName)
        {
            if (mixer == null)
            {
                return null;
            }

            var groups = mixer.FindMatchingGroups(groupName);
            for (var i = 0; i < groups.Length; i++)
            {
                if (groups[i].name == groupName)
                {
                    return groups[i];
                }
            }

            return groups.Length > 0 ? groups[0] : null;
        }

        private static void ConfigureMusicImporter(string path)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
            {
                Debug.LogWarning($"Music clip not found at '{path}'.");
                return;
            }

            var settings = importer.defaultSampleSettings;
            var changed = settings.loadType != AudioClipLoadType.Streaming ||
                          settings.compressionFormat != AudioCompressionFormat.Vorbis ||
                          !Mathf.Approximately(settings.quality, 0.75f) ||
                          !importer.loadInBackground ||
                          settings.preloadAudioData;
            if (!changed)
            {
                return;
            }

            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.75f;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.SaveAndReimport();
        }
    }
}
