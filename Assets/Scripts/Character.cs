using System.Collections;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace TempCompany.MissileGame
{
    public enum CharacterType
    {
        Player,
        Enemy,
    }
    public class Character : MonoBehaviourPunCallbacks, IPunObservable
    {
        private Rigidbody2D _rigidbody;
        private Slider _sliderHp;
        private Slider _sliderCooltime;
        private Text _nicknameText;
        
        public CharacterType characterType;
        public float MaxHealth = 3f;
        public float CurHealth = 3f;
        public float Cooltime = 1000f;
        public float CurTime = 0f;
        public bool canShot = false;

        private Stopwatch coolTimer = new();

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            Debug.Log($"Add Photon View({photonView.ViewID}) IsMine({photonView.IsMine})");
            CurHealth = MaxHealth;
            if (photonView.IsMine)
            {
                if (null == GameManager.LocalPlayerInstance)
                {
                    transform.position = new Vector3(0f, -3f, 0f);
                    GameManager.LocalPlayerInstance = this;
                    characterType = CharacterType.Player;

                    var player = Resources.Load("Prefabs/PlayerImage");
                    Instantiate(player, transform);
                }
            }
            else
            {
                if (null == GameManager.LocalEnemyInstance)
                {
                    transform.position = new Vector3(0f, 3f, 0f);
                    GameManager.LocalEnemyInstance = this;
                    characterType = CharacterType.Enemy;

                    var player = Resources.Load("Prefabs/EnemyImage");
                    Instantiate(player, transform);
                }
            }

            coolTimer.Start();
            InitUI();
        }

        void InitUI()
        {
            {
                var slider = Resources.Load<Slider>("Prefabs/UI/SliderHp");
                _sliderHp = Instantiate<Slider>(slider, GameManager.Instance.hudMain);
                _sliderHp.transform.position = transform.position;
            }

            {
                var slider = Resources.Load<Slider>("Prefabs/UI/SliderCooltime");
                _sliderCooltime = Instantiate<Slider>(slider, GameManager.Instance.hudMain);
                _sliderCooltime.transform.position = transform.position;
            }

            {
                var nicknameText = Resources.Load<Text>("Prefabs/UI/TextName");
                _nicknameText = Instantiate<Text>(nicknameText, GameManager.Instance.hudMain);
                _nicknameText.text = photonView.Controller.NickName;
            }
        }

        #region IPunObservable implementation

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(CurHealth);
                stream.SendNext(canShot);
                stream.SendNext(_rigidbody.velocity);
            }
            else
            {
                CurHealth = (float)stream.ReceiveNext();
                canShot = (bool)stream.ReceiveNext();
                _rigidbody.velocity = (Vector2)stream.ReceiveNext();

                if (canShot)
                    ReStartCooltime();
                
                float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                _rigidbody.position += _rigidbody.velocity * lag;
            }
        }

        #endregion

        void Update()
        {
            UpdatePlayer();
            UpdateShotMissile();
        }

        private void LateUpdate()
        {
            if (null == _sliderHp || null == _sliderCooltime || null == _nicknameText)
                return;
            Vector3 sliderPos = transform.position;
            Vector3 sliderCooltimePos = transform.position;
            Vector3 nickNamePos = transform.position;
            if (CharacterType.Enemy == characterType)
            {
                sliderPos.y += 1;
                sliderCooltimePos.y += 0.9f;
                nickNamePos.y += 0.6f;
            }
            if (CharacterType.Player == characterType)
            {
                sliderPos.y -= 1;
                sliderCooltimePos.y -= 0.9f;
                nickNamePos.y -= 0.6f;
            }
            _sliderHp.transform.position = sliderPos;
            _sliderCooltime.transform.position = sliderCooltimePos;
            _nicknameText.transform.position = nickNamePos;

            _sliderHp.value = CurHealth / MaxHealth;
            _sliderCooltime.value = CurTime / Cooltime;
        }

        private void UpdatePlayer()
        {
            if (CharacterType.Player != characterType)
                return;
            float h = Input.GetAxis("Horizontal");
            _rigidbody.velocity = new Vector2(h, 0);

        }

        private void UpdateShotMissile()
        {
            if (GameManager.Instance.isReceiveFinishGame)
                return;
            CurTime = (float)coolTimer.Elapsed.TotalMilliseconds;
            canShot = CurTime >= Cooltime;
            if (CharacterType.Player == characterType)
            {
                if (canShot)
                {
                    PhotonNetwork.Instantiate("Prefabs/Missile", Vector3.zero, Quaternion.identity, 0);
                    ReStartCooltime();
                }
            }
            else
            {
                if (canShot)
                {
                    ReStartCooltime();
                }
            }
        }

        private void ReStartCooltime()
        {
            coolTimer.Reset();
            CurTime = 0f;
            coolTimer.Start();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (PhotonNetwork.IsMasterClient && GameManager.Instance.isSendFinishGame)
                return;
            if (GameManager.Instance.isReceiveFinishGame)
                return;
            if (false ==IsLive())
                return;
            if (false == photonView.IsMine) // 내가 피격될 때만 피격 판정
                return;
            if (false == collision.CompareTag("Missile"))
                return;
            PhotonView missileView = collision.GetComponent<PhotonView>();
            if (missileView.ControllerActorNr == photonView.ControllerActorNr)
                return;
            Debug.Log($"isTrigger({collision.name})");
            CurHealth -= 1f;
        }

        public bool IsLive()
        {
            return CurHealth > 0;
        }
    }
}