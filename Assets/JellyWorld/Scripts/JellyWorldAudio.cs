using UnityEngine;
using UnityEngine.UI;
namespace JellyWorldGame
{
    public enum JellySound { Click,Hover,Swap,Pop,Invalid,IceBreak,Step,Impact,Shoot,Shuffle,Win,Lose,Jump,Beam }
    public partial class JellyWorld
    {
        public AudioClip[] soundClips=new AudioClip[14];
        AudioSource[] soundVoices;
        int soundVoice;
        float uiVolume=.65f,gameVolume=.70f,nextHoverSound,nextIceSound;
        Text audioToggleLabel;
        void InitializeAudio()
        {
            uiVolume=PlayerPrefs.GetFloat("JellyWorld.Audio.UI",.65f);
            gameVolume=PlayerPrefs.GetFloat("JellyWorld.Audio.Game",.70f);
            var voices=new System.Collections.Generic.List<AudioSource>(GetComponents<AudioSource>());
            while(voices.Count<8)voices.Add(gameObject.AddComponent<AudioSource>());
            soundVoices=voices.ToArray();
            foreach(var voice in soundVoices){voice.playOnAwake=false;voice.spatialBlend=0;voice.loop=false;}
        }
        public void PlaySound(JellySound kind,float gain=1,float pitch=1)
        {
            if(!Application.isPlaying || !sound || soundClips==null || (int)kind>=soundClips.Length)return;
            var clip=soundClips[(int)kind];if(clip==null)return;
            bool ui=kind==JellySound.Click || kind==JellySound.Hover;
            if((ui?uiVolume:gameVolume)<=0)return;
            if(kind==JellySound.Hover){if(Time.unscaledTime<nextHoverSound)return;nextHoverSound=Time.unscaledTime+.10f;}
            if(kind==JellySound.IceBreak){if(Time.unscaledTime<nextIceSound)return;nextIceSound=Time.unscaledTime+.08f;}
            if(soundVoices==null)InitializeAudio();
            AudioSource source=null;
            foreach(var candidate in soundVoices)if(!candidate.isPlaying){source=candidate;break;}
            if(source==null)source=soundVoices[(soundVoice++)%soundVoices.Length];
            source.Stop();source.clip=clip;source.pitch=Mathf.Clamp(pitch,.8f,1.35f);
            source.volume=(ui?uiVolume:gameVolume)*Mathf.Clamp01(gain);
            source.ignoreListenerPause=ui;source.Play();
        }
        // Compatibility for older gameplay calls. All playback now uses downloaded clips.
        void Tone(float hz,float duration)
        {
            if(hz<300)PlaySound(JellySound.Invalid,.35f);
            else if(duration<=.05f)PlaySound(JellySound.Click,.45f);
            else if(hz<500)PlaySound(JellySound.Swap,.5f);
            else PlaySound(JellySound.Pop,.50f,Mathf.Clamp(hz/720,.9f,1.2f));
        }
        void StoreAudioVolumes()
        {
            PlayerPrefs.SetFloat("JellyWorld.Audio.UI",uiVolume);PlayerPrefs.SetFloat("JellyWorld.Audio.Game",gameVolume);PlayerPrefs.Save();
        }
        public void OpenAudioSettings()
        {
            bool resume=mode=="play" || mode=="golem" || mode=="range";
            if(resume){paused=true;ResetBoardGesture();Time.timeScale=0;if(Guardian!=null)Guardian.SyncCursor();if(Range!=null)Range.SyncCursor();}
            ClearUIChildren(modal);modal.gameObject.SetActive(true);
            Panel("Audio veil",modal,Vector2.zero,new Vector2(6000,4000),new Color(.12f,.16f,.24f,.48f));
            var card=Panel("Audio settings card",modal,Vector2.zero,new Vector2(650,444),new Color(.98f,.99f,1)).transform;
            Label("Audio heading",card,"音效设置",new Vector2(0,158),new Vector2(558,50),34,Ink);
            AddVolumeSlider(card,"界面音量",49,uiVolume,value=>{uiVolume=value;StoreAudioVolumes();});
            AddVolumeSlider(card,"游戏音量",-32,gameVolume,value=>{gameVolume=value;StoreAudioVolumes();});
            var toggle=Button("Mute audio",card,sound?"音效：开":"音效：关",new Vector2(-146,-125),new Vector2(248,50),new Color(.85f,.93f,.91f),Ink,()=>{
                sound=!sound;PlayerPrefs.SetInt("JellyWorld.Sound",sound?1:0);PlayerPrefs.Save();
                audioToggleLabel.text=sound?"音效：开":"音效：关";
                if(!sound && soundVoices!=null)foreach(var voice in soundVoices)voice.Stop();
            },21);
            audioToggleLabel=toggle.GetComponentInChildren<Text>();
            Button("Preview audio",card,"试听碎冰",new Vector2(146,-125),new Vector2(248,50),new Color(.85f,.90f,.99f),Ink,()=>PlaySound(JellySound.IceBreak,.8f),21);
            Button("Close audio settings",card,resume?"返回游戏":"返回",new Vector2(0,-188),new Vector2(280,46),Purple,Color.white,()=>{
                modal.gameObject.SetActive(false);if(resume){paused=false;Time.timeScale=1;if(Guardian!=null)Guardian.SyncCursor();if(Range!=null)Range.SyncCursor();}
            },21);
        }
        void AddVolumeSlider(Transform card,string title,float y,float initial,System.Action<float> changed)
        {
            Label(title+" label",card,title,new Vector2(-185,y+22),new Vector2(170,32),20,Ink);
            var percent=Label(title+" value",card,Mathf.RoundToInt(initial*100)+"%",new Vector2(226,y+22),new Vector2(95,32),20,Purple,TextAnchor.MiddleRight);
            var rt=Rect(title+" slider",card,new Vector2(0,y-9),new Vector2(540,26));
            var slider=rt.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;
            var track=Panel("Slider track",rt,Vector2.zero,new Vector2(540,10),new Color(.82f,.86f,.91f));
            var fillArea=Rect("Slider fill area",rt,Vector2.zero,new Vector2(540,10));
            var fill=Panel("Slider fill",fillArea,Vector2.zero,Vector2.zero,new Color(.35f,.73f,.65f));
            var handleArea=Rect("Slider handle area",rt,Vector2.zero,new Vector2(516,24));
            var handle=Panel("Slider handle",handleArea,Vector2.zero,new Vector2(24,0),Purple);
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.direction=Slider.Direction.LeftToRight;slider.value=initial;
            slider.onValueChanged.AddListener(value=>{percent.text=Mathf.RoundToInt(value*100)+"%";changed(value);});
        }
    }
}
