using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace ICKX.AudioRecorder {
	
	/// <summary>
	/// AudioListenerかAudioSourceにアタッチすることで録音可能にする
	/// </summary>
	public class AudioComponentRecorder : MonoBehaviour {
		
		private const int HeaderSize = 44;

		[SerializeField]
		private int _DefaultCopyBufferSize = 8092;

		private FileStream _FileStream;
		private float[] _AudioReadBuffer;
		private float[] _CopyBuffer;
		private byte[] _WriteBuffer;
		private int _AudioReadBufferPos = 0;


		private readonly object balanceLock = new object ();
		private Thread _WriteThread;

		public ushort RecordChannels { get; private set; }
		public ushort BitPerSample { get; private set; }
		public int SamplingRate { get; private set; }

		private int OutputSampleRate;
		public bool IsRecording { get; private set; } = false;

		private void OnAudioFilterRead (float[] data, int channels) {
			if (!IsRecording || _AudioReadBuffer == null) return;
			if (RecordChannels != channels) {
				Debug.LogError ("speakerModeが変更されたので録音できません");
				IsRecording = false;
				return;
			}

			lock (balanceLock) {
				if(_AudioReadBufferPos + data.Length > _AudioReadBuffer.Length) {
					System.Array.Resize (ref _AudioReadBuffer, _AudioReadBuffer.Length * 2);
					System.Array.Resize (ref _CopyBuffer, _CopyBuffer.Length * 2);
					Debug.Log ("resize : _AudioReadBuffer " + _AudioReadBuffer.Length);
				}

				System.Array.Copy (data, 0, _AudioReadBuffer, _AudioReadBufferPos, data.Length);
				_AudioReadBufferPos += data.Length;
			}
		}

		private void OnDestroy () {
			if(IsRecording) {
				StopRecording ();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void StartRecording (string fileName, int bitPerSampleCompress = 2, int samplingRateCompress = 1) {
			if (IsRecording) return;

			if (bitPerSampleCompress < 0 || bitPerSampleCompress > 3) {
				throw new System.NotSupportedException ();
			}
			if (samplingRateCompress < 0) {
				throw new System.NotSupportedException ();
			}

			switch (AudioSettings.speakerMode) {
				case AudioSpeakerMode.Mono:
					RecordChannels = 1;
					break;
				case AudioSpeakerMode.Stereo:
					RecordChannels = 2;
					break;
				default:
					throw new System.NotSupportedException ();
			}

			IsRecording = true;

			OutputSampleRate = AudioSettings.outputSampleRate;
			BitPerSample = (ushort)(32 / Mathf.Pow (2, bitPerSampleCompress));
			SamplingRate = (int)(OutputSampleRate / Mathf.Pow(2, samplingRateCompress));

			if (_AudioReadBuffer == null) {
				_AudioReadBuffer = new float[_DefaultCopyBufferSize * 2];
			}
			if (_CopyBuffer == null) {
				_CopyBuffer = new float[_DefaultCopyBufferSize];
			}

			int writeBufferSize = (int)(_CopyBuffer.Length * (BitPerSample / 8) / Mathf.Pow (2, samplingRateCompress));
			if (_WriteBuffer == null || _WriteBuffer.Length != writeBufferSize) {
				_WriteBuffer = new byte[writeBufferSize];
			}

			_FileStream = new FileStream (fileName, FileMode.Create);
			var emptyByte = default (byte);
			for (int i = 0; i < HeaderSize; i++) {
				_FileStream.WriteByte (emptyByte);
			}

			_WriteThread = new Thread (new ThreadStart (WriteThreadMethod));
			_WriteThread.Start ();
		}

		private unsafe void WriteThreadMethod () {

			while (true) {
				int currentPos = 0;
				int copyBufferLen = 0;
				lock (balanceLock) {
					currentPos = _AudioReadBufferPos;
					copyBufferLen = _CopyBuffer.Length;
				}

				if (!IsRecording && currentPos == 0) break;

				//データが溜まるか、録音が終わったら書き込み
				if (!IsRecording || currentPos >= copyBufferLen) {
					int rate = OutputSampleRate / SamplingRate;
					int dataCount = (Mathf.Min(copyBufferLen, currentPos) / rate) / RecordChannels;
					int writeLen = RecordChannels * dataCount * (BitPerSample / 8);

					if (writeLen > _WriteBuffer.Length) {
						System.Array.Resize (ref _WriteBuffer, _WriteBuffer.Length * 2);
						Debug.Log ("resize : _WriteBuffer " + _WriteBuffer.Length + ", " + writeLen + ", " + copyBufferLen + "," + currentPos);
					}

					//copyBufferLen以下の_AudioReadBufferの値は OnAudioFilterReadで書き換えないのでlockしない
					switch (BitPerSample) {
						case 8:
							for (int i = 0; i < dataCount; i++) {
								for (int j = 0; j < RecordChannels; j++) {
									float value = _AudioReadBuffer[i * rate * RecordChannels + j];
									_WriteBuffer[i * RecordChannels + j] = (byte)(value * sbyte.MaxValue);
								}
							}
							break;
						case 16:
							for (int i = 0; i < dataCount; i++) {
								for (int j = 0; j < RecordChannels; j++) {
									float value = _AudioReadBuffer[i * rate * RecordChannels + j];
									short shortVal = (short)(value * short.MaxValue);
									var ptr = (byte*)&shortVal;
									if (BitConverter.IsLittleEndian) {
										_WriteBuffer[(i * RecordChannels + j) * 2 + 0] = ptr[0];
										_WriteBuffer[(i * RecordChannels + j) * 2 + 1] = ptr[1];
									} else {
										_WriteBuffer[(i * RecordChannels + j) * 2 + 0] = ptr[1];
										_WriteBuffer[(i * RecordChannels + j) * 2 + 1] = ptr[0];
									}
								}
							}
							break;
						case 32:
							for (int i = 0; i < dataCount; i++) {
								for (int j = 0; j < RecordChannels; j++) {
									float value = _AudioReadBuffer[i * rate * RecordChannels + j];
									var ptr = (byte*)&value;
									if (BitConverter.IsLittleEndian) {
										_WriteBuffer[(i * RecordChannels + j) * 4 + 0] = ptr[0];
										_WriteBuffer[(i * RecordChannels + j) * 4 + 1] = ptr[1];
										_WriteBuffer[(i * RecordChannels + j) * 4 + 2] = ptr[2];
										_WriteBuffer[(i * RecordChannels + j) * 4 + 3] = ptr[3];
									} else {
										_WriteBuffer[(i * RecordChannels + j) * 4 + 0] = ptr[3];
										_WriteBuffer[(i * RecordChannels + j) * 4 + 1] = ptr[2];
										_WriteBuffer[(i * RecordChannels + j) * 4 + 2] = ptr[1];
										_WriteBuffer[(i * RecordChannels + j) * 4 + 3] = ptr[0];
									}
								}
							}
							break;
					}

					_FileStream.Write (_WriteBuffer, 0, writeLen);

					lock (balanceLock) {
						//読み終わった_AudioReadBufferを削る
						System.Array.Copy (_AudioReadBuffer, _CopyBuffer.Length, _CopyBuffer, 0, _CopyBuffer.Length);
						System.Array.Copy (_CopyBuffer, 0, _AudioReadBuffer, 0, _CopyBuffer.Length);
						_AudioReadBufferPos -= _CopyBuffer.Length;
						if (_AudioReadBufferPos < 0) _AudioReadBufferPos = 0;
					}
					//Debug.Log ($"{currentPos} {_AudioReadBufferPos} {BitConverter.IsLittleEndian}");
				} else {
					Thread.Sleep (10);
				}
			}

			//headerを書き込み
			_FileStream.Seek (0, SeekOrigin.Begin);

			//ヘッダー書くだけならBitConverter使っても大丈夫でしょ
			Byte[] riff = Encoding.UTF8.GetBytes ("RIFF");
			_FileStream.Write (riff, 0, 4);
			Byte[] chunkSize = BitConverter.GetBytes (_FileStream.Length - 8);
			_FileStream.Write (chunkSize, 0, 4);
			Byte[] wave = Encoding.UTF8.GetBytes ("WAVE");
			_FileStream.Write (wave, 0, 4);
			Byte[] fmt = Encoding.UTF8.GetBytes ("fmt ");
			_FileStream.Write (fmt, 0, 4);
			Byte[] subChunk1 = BitConverter.GetBytes (16);
			_FileStream.Write (subChunk1, 0, 4);
			
			Byte[] audioFormat = BitConverter.GetBytes ((UInt16)1);
			_FileStream.Write (audioFormat, 0, 2);
			Byte[] numChannels = BitConverter.GetBytes (RecordChannels);
			_FileStream.Write (numChannels, 0, 2);
			Byte[] sampleRate = BitConverter.GetBytes (SamplingRate);
			_FileStream.Write (sampleRate, 0, 4);

			Byte[] byteRate = BitConverter.GetBytes (SamplingRate * (BitPerSample / 8) * RecordChannels);
			// sampleRate * bytesPerSample*number of channels, here 44100*2*2
			_FileStream.Write (byteRate, 0, 4);

			Byte[] blockAlign = BitConverter.GetBytes (RecordChannels * BitPerSample/8);
			_FileStream.Write (blockAlign, 0, 2);

			Byte[] bitPerSample = BitConverter.GetBytes (BitPerSample);
			_FileStream.Write (bitPerSample, 0, 2);

			Byte[] dataString = Encoding.UTF8.GetBytes ("data");
			_FileStream.Write (dataString, 0, 4);

			Byte[] subChunk2 = BitConverter.GetBytes (_FileStream.Length - HeaderSize);
			_FileStream.Write (subChunk2, 0, 4);

			_FileStream.Close ();
		}

		/// <summary>
		/// 
		/// </summary>
		public void StopRecording () {
			if (!IsRecording) return;
			IsRecording = false;
		}
	}
}