using System.Collections;
using UnityEngine;
using Photon.Pun;

namespace TempCompany.MissileGame
{
    public class Missile : MonoBehaviourPunCallbacks, IPunObservable
    {
        public float speed = 3f;
        private Rigidbody2D _rigidbody;

        float lag = 0f;
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            if (photonView.IsMine)
            {
                transform.position = GameManager.LocalPlayerInstance.transform.position;
            }
            else
            {
                transform.position = GameManager.LocalEnemyInstance.transform.position;
            }
        }

        #region IPunObservable implementation

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(_rigidbody.velocity);
            }
            else
            {
                Vector2 enemyBulletVelocity = (Vector2)stream.ReceiveNext();
                
                enemyBulletVelocity.y *= -1f;
                _rigidbody.velocity = enemyBulletVelocity;

                lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                _rigidbody.position += _rigidbody.velocity * lag;
            }
        }
        #endregion

        void Update()
        {
            if (photonView.IsMine)
                GetComponent<Rigidbody2D>().velocity = new Vector2(0, speed);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (false == photonView.IsMine)
                return;
            if (collision.CompareTag("Missile"))
                return;
            if (collision.TryGetComponent<Character>(out var character))
            {
                if (character.photonView.IsMine)
                    return;

                PhotonNetwork.Destroy(photonView);
            }
            else
            {
                PhotonNetwork.Destroy(photonView);
            }
        }
    }
}