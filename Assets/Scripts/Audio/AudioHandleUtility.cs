using UnityEngine;

public static class AudioHandleUtility
{
    public static void ApplyAudioData(AudioSource source, AudioData data, AudioAssetSO asset)
    {
        source.clip = asset.Clip;
        source.outputAudioMixerGroup = asset.MixerGroup;
        source.loop = data.PlayMode == SfxPlayMode.Loop;

        float volume = asset.BaseVolume + data.Volume;
        if (data.VolumeDelta > 0f)
            volume += Random.Range(-data.VolumeDelta, data.VolumeDelta);
        source.volume = Mathf.Clamp01(volume);

        source.pitch = data.PitchDelta > 0f ? 1f + Random.Range(-data.PitchDelta, data.PitchDelta) : 1f;
    }
}