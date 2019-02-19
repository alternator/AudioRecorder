# AudioRecorder

AudioComponentRecorderをAudioListerやAudioSourceのあるGameObjectにアタッチすることで、Wavフォーマットで録音ができます。

## インストール方法

### PackageManagerの場合
manifest.jsonに以下の一文をPackegeに追加すればインストール完了します
```
"jp.ickx.audiorecorder": "https://github.com/alternator/AudioRecorder.git",
```

### 使わない場合
Scripts/Runtime フォルダにあるAudioComponentRecorder.csをAssetsフォルダ以下にインポートしてください。

## AudioComponentRecorder
### 使い方

DefaultCopyBufferSizeの値の分録音するデータを一度バッファに格納し、この値ごとに録音したデータがファイルに書き込まれます  (頻度が細かすぎて書き込みが間に合わない場合は自動で伸長します)

録音開始 は StartRecording (fileName, bitPerSampleCompress, samplingRateCompress)
録音停止 は StopRecording ()

bitPerSampleCompressの値は、0で32bit, 1で16bit, 2で8bitのビット深度で録音
samplingRateCompressの値は、AudioSettings.outputSampleRate / (2 ^ value) のサンプル数で録音します
