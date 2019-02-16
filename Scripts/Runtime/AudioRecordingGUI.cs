using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ICKX.AudioRecorder {

	public class AudioRecordingGUI : MonoBehaviour {
		[SerializeField]
		private string _FilePath = "test.wav";
		[SerializeField]
		private AudioComponentRecorder _AudioComponentRecorder;
		[SerializeField]
		private int BitParSampleCompressLevel = 2;
		[SerializeField]
		private int SamplingRateCompressLevel = 1;

		void OnGUI () {
			GUILayout.BeginVertical ("box", GUILayout.Width(300.0f));

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("FilePath");
			_FilePath = GUILayout.TextField (_FilePath);
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Start")) {
				_AudioComponentRecorder.StartRecording (_FilePath, BitParSampleCompressLevel, SamplingRateCompressLevel);
			}
			if (GUILayout.Button ("Stop")) {
				_AudioComponentRecorder.StopRecording ();
			}
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical ();
		}
	}
}