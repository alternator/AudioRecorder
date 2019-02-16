# AudioRecorder

AudioComponentRecorderをAudioListerやAudioSourceのあるGameObjectにアタッチすることで、Wavフォーマットで録音ができます。

## AudioComponentRecorder

### 使い方

DefaultCopyBufferSize の値は録音するデータを一度バッファに格納し、この値ごとに録音したデータがファイルに書き込まれます  (頻度が細かすぎて書き込みが間に合わない場合は自動で伸長します)

録音開始 は StartRecording (fileName, bitPerSampleCompress, samplingRateCompress)
録音停止 は StopRecording ()

bitPerSampleCompressの値は、0で32bit, 1で16bit, 2で8bitのビット深度で録音
samplingRateCompressの値は、AudioSettings.outputSampleRate / (2 ^ value) のサンプル数で録音します
