using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BgmType
{
    Lobby,
    Battle,
}


public enum SfxType
{
    Button,
    Installation,
    Rotation,
    
    PlayerHit,
    PlayerDie,
    
    Enemy_Stone,
    
    AllRoundFiring,
    Bow,
    ChineseCleaver,
    DrawSword,
    ElectricBall,
    Esper,
    FlametHrower,
    FuryCutter,
    Greatsword,
    GrenadeLauncher,
    Gun,
    Hatchet,
    LightningAura,
    MachineGun,
    MagicCircle,
    Rapier,
    RocketLauncher,
    ShotGun,
    Sniper,
    Spear,
    Sword,
    Sword2,
    SwordCurtain,
    Uzi,
    Wind,
    Zergling,
    
    explosion,
    explosion2,
    explosion3,
    explosion4,
    
    
}

public enum SoundVolumeType
{
    Master,
    BGM,
    SFX,
}

public class SoundManager : MonoBehaviour
{
    // 싱글톤 인스턴스. 다른 스크립트에서 SoundManager.Instance로 접근함.
    public static SoundManager Instance { get; private set; }

    [Tooltip("SFX 동시 재생을 위해 미리 생성할 AudioSource 개수")]
    [SerializeField] private int sfxPoolSize = 10;

    [Tooltip("Resources 폴더 기준 BGM 경로")]
    [SerializeField] private string bgmResourcePath = "Sounds/BGM";

    [Tooltip("Resources 폴더 기준 SFX 경로")]
    [SerializeField] private string sfxResourcePath = "Sounds/SFX";

    // BGM 전용 AudioSource. BGM은 하나만 재생되도록 관리함.
    private AudioSource bgmSource;
    // 곡이 끝날 때 같은 BGM 타입 안에서 다음 랜덤 곡을 자동 재생할지 여부
    private bool playRandomNextBgm;
    // SFX용 AudioSource 풀. 효과음이 여러 개 겹쳐도 재생할 수 있도록 여러 개를 만들어둠.
    private List<AudioSource> sfxSources = new List<AudioSource>();

    // BGM 타입별 오디오 클립 목록.
    private Dictionary<BgmType, List<AudioClip>> bgmClips = new Dictionary<BgmType, List<AudioClip>>();

    // SFX 타입별 오디오 클립 목록.
    private Dictionary<SfxType, List<AudioClip>> sfxClips = new Dictionary<SfxType, List<AudioClip>>();

    // BGM 페이드 코루틴을 저장해서 중복 실행을 막음.
    private Coroutine bgmFadeCoroutine;

    // 현재 재생 중인 BGM 타입과 클립을 저장함.
    private BgmType? currentBgmType;
    private AudioClip currentBgmClip;

    // 현재 볼륨 값. UI 슬라이더와 연결할 때 사용 가능.
    public float MasterVolume { get; private set; } = 1f;
    public float BgmVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;

    // PlayerPrefs에 저장할 키 값.
    private const string MasterVolumeKey = "Sound_MasterVolume";
    private const string BgmVolumeKey = "Sound_BgmVolume";
    private const string SfxVolumeKey = "Sound_SfxVolume";

    //=================================================================================

    private void Awake()
    {
        // 이미 SoundManager가 존재하면 중복 생성된 오브젝트 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 싱글톤 등록
        Instance = this;

        // 씬이 바뀌어도 사운드 매니저가 유지되도록 설정
        DontDestroyOnLoad(gameObject);

        // BGM / SFX AudioSource 생성
        CreateAudioSources();

        // 저장된 볼륨 값 불러오기
        LoadVolumeSettings();

        // Resources 폴더에서 사운드 클립 자동 로드
        LoadAllSounds();

        // 불러온 볼륨 값을 실제 AudioSource에 적용
        ApplyAllVolumes();
    }

    // 랜덤 이어듣기 사용 시 현재 BGM이 끝나면 다음 랜덤 곡을 자동 재생
    private void Update()
    {
        // 랜덤 이어듣기를 사용하지 않으면 종료
        if (!playRandomNextBgm)
        {
            return;
        }

        // BGM AudioSource가 없으면 종료
        if (bgmSource == null)
        {
            return;
        }

        // 반복 재생 중이면 종료
        if (bgmSource.loop)
        {
            return;
        }

        // 아직 현재 곡이 재생 중이면 종료
        if (bgmSource.isPlaying)
        {
            return;
        }

        // 현재 재생 중인 BGM 타입이 없으면 종료
        if (!currentBgmType.HasValue)
        {
            return;
        }

        // 같은 BGM 타입 안에서 다음 랜덤 곡 재생
        PlayBGM(currentBgmType.Value, -1, true, true);
    }
    
    //=================================================================================

    // BGM용 AudioSource 1개와 SFX용 AudioSource 풀을 생성
    private void CreateAudioSources()
    {
        // BGM용 AudioSource 생성
        bgmSource = gameObject.AddComponent<AudioSource>();

        // BGM이 오브젝트 생성과 동시에 자동 재생되지 않도록 설정
        bgmSource.playOnAwake = false;

        // BGM은 반복 재생되도록 설정
        bgmSource.loop = true;

        // 설정한 개수만큼 SFX용 AudioSource 생성
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();

            // SFX도 자동 재생되지 않도록 설정
            source.playOnAwake = false;

            // 효과음은 기본적으로 반복 재생하지 않음
            source.loop = false;

            // 생성한 AudioSource를 풀에 저장
            sfxSources.Add(source);
        }
    }

    //=================================================================================

    // Resources 폴더에서 BGM / SFX를 enum 이름 기준으로 자동 로드
    private void LoadAllSounds()
    {
        // 혹시 기존 데이터가 있다면 초기화
        bgmClips.Clear();
        sfxClips.Clear();

        // BgmType enum에 등록된 모든 값을 순회
        foreach (BgmType bgmType in System.Enum.GetValues(typeof(BgmType)))
        {
            // 예: Resources/Sounds/BGM/Lobby
            string path = $"{bgmResourcePath}/{bgmType}";

            // 해당 폴더 안의 모든 AudioClip을 로드하고 이름순으로 정렬
            AudioClip[] clips = Resources.LoadAll<AudioClip>(path).OrderBy(clip => clip.name).ToArray();

            // 클립이 하나라도 있으면 딕셔너리에 등록
            if (clips.Length > 0)
            {
                bgmClips.Add(bgmType, new List<AudioClip>(clips));
            }
        }

        // SfxType enum에 등록된 모든 값을 순회
        foreach (SfxType sfxType in System.Enum.GetValues(typeof(SfxType)))
        {
            // 예: Resources/Sounds/SFX/Attack
            string path = $"{sfxResourcePath}/{sfxType}";

            // 해당 폴더 안의 모든 AudioClip을 로드하고 이름순으로 정렬
            AudioClip[] clips = Resources.LoadAll<AudioClip>(path).OrderBy(clip => clip.name).ToArray();

            // 클립이 하나라도 있으면 딕셔너리에 등록
            if (clips.Length > 0)
            {
                sfxClips.Add(sfxType, new List<AudioClip>(clips));
            }
        }
    }

    //=================================================================================
    
    // BGM 재생
    // index가 -1이면 해당 BGM 타입 안에서 랜덤 재생
    // randomNext가 true이면 현재 곡이 끝난 뒤 같은 BGM 타입의 랜덤 곡을 이어서 재생
    public void PlayBGM(
        BgmType bgmType,
        int index = -1,
        bool restartSameBgm = false,
        bool randomNext = false)
    {
        // 재생할 BGM 클립 가져오기
        AudioClip clip = GetBgmClip(bgmType, index);

        // 클립을 못 찾으면 실행 중단
        if (clip == null)
        {
            return;
        }

        // 같은 BGM이 이미 재생 중이면 다시 처음부터 재생하지 않음
        if (!restartSameBgm && currentBgmClip == clip && bgmSource.isPlaying)
        {
            return;
        }

        // 현재 BGM 정보 저장
        currentBgmType = bgmType;
        currentBgmClip = clip;

        // 곡 종료 후 랜덤으로 다음 곡을 재생할지 저장
        playRandomNextBgm = randomNext;

        // BGM 설정
        bgmSource.clip = clip;
        bgmSource.volume = BgmVolume;

        // randomNext가 true면 곡 종료를 감지해야 하므로 반복 재생을 끔
        bgmSource.loop = !randomNext;

        // BGM 재생
        bgmSource.Play();
    }

    // BGM을 페이드 전환으로 재생
    // 기존 BGM을 서서히 줄이고 새 BGM을 서서히 키움
    public void PlayBGMFade(BgmType bgmType, float fadeDuration = 1f, int index = -1, bool restartSameBgm = false)
    {
        // 재생할 BGM 클립 가져오기
        AudioClip clip = GetBgmClip(bgmType, index);

        // 클립을 못 찾으면 실행 중단
        if (clip == null)
        {
            return;
        }

        // 같은 BGM이 이미 재생 중이면 다시 재생하지 않음
        if (!restartSameBgm && currentBgmClip == clip && bgmSource.isPlaying)
        {
            return;
        }

        // 기존 페이드 코루틴이 있으면 중단
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        // 새 페이드 코루틴 시작
        bgmFadeCoroutine = StartCoroutine(CoPlayBGMFade(bgmType, clip, fadeDuration));
    }

    // 실제 BGM 페이드 처리를 담당하는 코루틴
    private IEnumerator CoPlayBGMFade(BgmType bgmType, AudioClip nextClip, float fadeDuration)
    {
        // 0초가 들어와도 계산 오류가 나지 않도록 최소 시간 보정
        float duration = Mathf.Max(0.01f, fadeDuration);

        // 기존 BGM이 재생 중이면 먼저 페이드 아웃
        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;

            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
                yield return null;
            }
        }

        // 현재 BGM 정보 갱신
        currentBgmType = bgmType;
        currentBgmClip = nextClip;

        // 새 BGM을 볼륨 0 상태로 시작
        bgmSource.Stop();
        bgmSource.clip = nextClip;
        bgmSource.volume = 0f;
        bgmSource.Play();

        // 새 BGM 페이드 인
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, BgmVolume, time / duration);
            yield return null;
        }

        // 최종 볼륨 보정
        bgmSource.volume = BgmVolume;

        // 코루틴 종료 표시
        bgmFadeCoroutine = null;
    }

    // 현재 BGM 정지
    public void StopBGM()
    {
        // 페이드 중이면 중단
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = null;
        }

        // BGM 정지 및 현재 정보 초기화
        bgmSource.Stop();
        bgmSource.clip = null;
        currentBgmType = null;
        currentBgmClip = null;
        playRandomNextBgm = false;
    }

    // 현재 BGM을 페이드 아웃 후 정지
    public void StopBGMFade(float fadeDuration = 1f)
    {
        // 기존 페이드 코루틴이 있으면 중단
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        // 페이드 아웃 코루틴 시작
        bgmFadeCoroutine = StartCoroutine(CoStopBGMFade(fadeDuration));
    }

    // 실제 BGM 페이드 아웃 정지를 담당하는 코루틴
    private IEnumerator CoStopBGMFade(float fadeDuration)
    {
        // 0초가 들어와도 계산 오류가 나지 않도록 최소 시간 보정
        float duration = Mathf.Max(0.01f, fadeDuration);

        // 현재 볼륨에서 0까지 서서히 감소
        float startVolume = bgmSource.volume;

        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        // 페이드가 끝나면 완전히 정지
        StopBGM();
    }

    //=================================================================================

    // SFX 재생
    // index가 -1이면 해당 SFX 타입 안에서 랜덤 재생
    public void PlaySFX(SfxType sfxType, int index = -1)
    {
        // 재생할 SFX 클립 가져오기
        AudioClip clip = GetSfxClip(sfxType, index);

        // 클립을 못 찾으면 실행 중단
        if (clip == null)
        {
            return;
        }

        // 현재 사용 가능한 SFX AudioSource 가져오기
        AudioSource source = GetAvailableSfxSource();

        // 현재 SFX 볼륨 적용
        source.volume = SfxVolume;

        // 효과음 1회 재생
        source.PlayOneShot(clip, SfxVolume);
    }

    // 같은 SFX를 여러 번 반복해서 재생
    // 예: 동전 여러 개 획득, 연속 타격음 등
    public void PlaySFXRepeated(SfxType sfxType, int count, float interval = 0.05f, int index = -1)
    {
        StartCoroutine(CoPlaySFXRepeated(sfxType, count, interval, index));
    }

    // SFX 반복 재생 코루틴
    private IEnumerator CoPlaySFXRepeated(SfxType sfxType, int count, float interval, int index)
    {
        // 음수가 들어오지 않도록 보정
        int playCount = Mathf.Max(0, count);

        // 대기 시간이 음수가 되지 않도록 보정
        float delay = Mathf.Max(0f, interval);

        for (int i = 0; i < playCount; i++)
        {
            PlaySFX(sfxType, index);
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    // 현재 사용 가능한 SFX AudioSource를 가져옴
    private AudioSource GetAvailableSfxSource()
    {
        // 재생 중이 아닌 AudioSource를 찾아서 반환
        for (int i = 0; i < sfxSources.Count; i++)
        {
            if (!sfxSources[i].isPlaying)
            {
                return sfxSources[i];
            }
        }

        // 전부 사용 중이면 첫 번째 AudioSource를 재사용
        // 너무 많은 효과음이 동시에 재생될 때를 대비한 안전 처리
        return sfxSources[0];
    }

    //=================================================================================

    // BGM 타입과 인덱스에 맞는 AudioClip 가져오기
    private AudioClip GetBgmClip(BgmType bgmType, int index)
    {
        // 해당 타입의 BGM이 등록되어 있는지 확인
        if (!bgmClips.TryGetValue(bgmType, out List<AudioClip> clips) || clips.Count == 0)
        {
            return null;
        }

        // index가 -1이면 랜덤 클립 반환
        if (index < 0)
        {
            return clips[Random.Range(0, clips.Count)];
        }

        // index가 클립 개수를 넘어가면 오류 방지를 위해 null 반환
        if (index >= clips.Count)
        {
            return null;
        }

        // 지정한 index의 클립 반환
        return clips[index];
    }

    // SFX 타입과 인덱스에 맞는 AudioClip 가져오기
    private AudioClip GetSfxClip(SfxType sfxType, int index)
    {
        // 해당 타입의 SFX가 등록되어 있는지 확인
        if (!sfxClips.TryGetValue(sfxType, out List<AudioClip> clips) || clips.Count == 0)
        {
            return null;
        }

        // index가 -1이면 랜덤 클립 반환
        if (index < 0)
        {
            return clips[Random.Range(0, clips.Count)];
        }

        // index가 클립 개수를 넘어가면 오류 방지를 위해 null 반환
        if (index >= clips.Count)
        {
            return null;
        }

        // 지정한 index의 클립 반환
        return clips[index];
    }

    //=================================================================================

    // Master / BGM / SFX 볼륨을 타입으로 구분해서 설정
    // UI 슬라이더 하나의 공용 함수로 연결할 때 사용 가능
    public void SetVolume(SoundVolumeType volumeType, float volume)
    {
        switch (volumeType)
        {
            case SoundVolumeType.Master:
                SetMasterVolume(volume);
                break;

            case SoundVolumeType.BGM:
                SetBgmVolume(volume);
                break;

            case SoundVolumeType.SFX:
                SetSfxVolume(volume);
                break;
        }
    }

    // 전체 볼륨 설정
    public void SetMasterVolume(float volume)
    {
        // 0~1 사이 값으로 보정
        MasterVolume = Mathf.Clamp01(volume);

        // Unity 전체 오디오 볼륨에 적용
        AudioListener.volume = MasterVolume;

        // 설정값 저장
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        PlayerPrefs.Save();
    }

    // BGM 볼륨 설정
    public void SetBgmVolume(float volume)
    {
        // 0~1 사이 값으로 보정
        BgmVolume = Mathf.Clamp01(volume);

        // BGM AudioSource에 적용
        if (bgmSource != null)
        {
            bgmSource.volume = BgmVolume;
        }

        // 설정값 저장
        PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
        PlayerPrefs.Save();
    }

    // SFX 볼륨 설정
    public void SetSfxVolume(float volume)
    {
        // 0~1 사이 값으로 보정
        SfxVolume = Mathf.Clamp01(volume);

        // 모든 SFX AudioSource에 적용
        foreach (AudioSource source in sfxSources)
        {
            source.volume = SfxVolume;
        }

        // 설정값 저장
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
    }

    // 저장된 볼륨 설정 불러오기
    private void LoadVolumeSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    // 현재 볼륨 값을 실제 AudioSource에 적용
    private void ApplyAllVolumes()
    {
        // 전체 볼륨 적용
        AudioListener.volume = MasterVolume;

        // BGM 볼륨 적용
        if (bgmSource != null)
        {
            bgmSource.volume = BgmVolume;
        }

        // SFX 볼륨 적용
        foreach (AudioSource source in sfxSources)
        {
            source.volume = SfxVolume;
        }
    }

    //=================================================================================

    // 특정 BGM이 현재 재생 중인지 확인
    public bool IsPlayingBGM(BgmType bgmType)
    {
        return currentBgmType.HasValue && currentBgmType.Value == bgmType && bgmSource.isPlaying;
    }
    
    //=================================================================================

    // 반복 재생되는 SFX 시작
    public AudioSource PlayLoopSFX(SfxType sfxType)
    {
        // 재생할 SFX 가져오기
        AudioClip clip = GetSfxClip(sfxType, -1);

        // 클립이 없으면 실행 중단
        if (clip == null)
            return null;

        // 루프 재생 전용 AudioSource 생성
        AudioSource source = gameObject.AddComponent<AudioSource>();

        // 클립 설정
        source.clip = clip;

        // 반복 재생 활성화
        source.loop = true;

        // 시작 시 자동 재생 방지
        source.playOnAwake = false;

        // 현재 SFX 볼륨 적용
        source.volume = SfxVolume;

        // 재생 시작
        source.Play();

        // 나중에 정지할 수 있도록 AudioSource 반환
        return source;
    }

// 반복 재생 중인 SFX 정지
    public void StopLoopSFX(AudioSource source)
    {
        // 대상이 없으면 종료
        if (source == null)
            return;

        // 재생 중지
        source.Stop();

        // 임시로 생성한 AudioSource 제거
        Destroy(source);
    }
// 모든 사운드 정지    
    public void StopAllSounds()
    {
        StopAllCoroutines();
        bgmFadeCoroutine = null;

        StopBGM();

        foreach (AudioSource source in sfxSources)
        {
            if (source == null)
                continue;

            source.Stop();
        }

        AudioSource[] allSources =
            GetComponents<AudioSource>();

        foreach (AudioSource source in allSources)
        {
            if (source == null ||
                source == bgmSource ||
                sfxSources.Contains(source))
            {
                continue;
            }

            source.Stop();
            Destroy(source);
        }
    }
}
