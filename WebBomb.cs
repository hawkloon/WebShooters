using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace WebShooters
{
    internal class WebBomb : WebShooterMode
    {
        public override string Name => "Web Bomb";

        public override string Description => "Throw off mutliple foes using your webs as a bomb!";

        public override string IconID => "WebShooter.WebBombIcon";

        public override bool RunHitAfterCreatureHit => true;
        public override bool ShootWeb => false;

        public override void OnActivate(WebShooterMono shooterMono)
        {
            base.OnActivate(shooterMono);
            Catalog.GetData<ItemData>("WebShooter.WebBomb").SpawnAsync(bomb =>
            {
                var hand = shooterMono.getHand();
                hand.Grab(bomb.GetMainHandle(hand.side), true);
                bomb.gameObject.AddComponent<WebBombMono>().shooterMono = shooterMono;
            }, shooterMono.item.transform.position, shooterMono.transform.rotation);
        }
    }

    public class WebBombMono : MonoBehaviour
    {
        Item item;
        public WebShooterMono shooterMono;

        public void Start()
        {
            item = GetComponent<Item>();
            item.mainCollisionHandler.OnCollisionStartEvent += MainCollisionHandler_OnCollisionStartEvent;
        }

        private void MainCollisionHandler_OnCollisionStartEvent(CollisionInstance collisionInstance)
        {
            if(collisionInstance.intensity > 0.5f)
            {
                foreach (Creature creature in Creature.allActive)
                {
                    if (creature.isPlayer || creature.isKilled) continue;

                    if (Vector3.Distance(creature.ragdoll.rootPart.transform.position, collisionInstance.contactPoint) > 10f) continue;

                    if (Physics.Raycast(collisionInstance.contactPoint, (creature.ragdoll.targetPart.transform.position - collisionInstance.contactPoint).normalized, out RaycastHit hit2, 20f))
                    {
                        var c = Extensions.GetCreatureFromRaycast(hit2);
                        if (!c) continue;

                        var webGame = shooterMono.webGameObject;
                        var ins = GameObject.Instantiate(webGame, creature.ragdoll.targetPart.transform);
                        ins.transform.position = hit2.point;
                        ins.transform.localRotation = Quaternion.LookRotation(hit2.normal);

                        Extensions.CalculateRandomScale(ins, true, 1f, 3f);
                        if (c.ragdoll.state == Ragdoll.State.NoPhysic) c.ragdoll.AddPhysicToggleModifier(this);
                        var pushDir = (c.ragdoll.rootPart.transform.position - collisionInstance.contactPoint).normalized;
                        c.ragdoll.SetState(Ragdoll.State.Destabilized);
                        c.ragdoll.targetPart.physicBody.AddForce(pushDir * 100, ForceMode.Impulse);
                    }
                }
                var web = GameObject.Instantiate(shooterMono.webGameObject);
                web.transform.position = collisionInstance.contactPoint;
                web.transform.localRotation = Quaternion.LookRotation(collisionInstance.contactNormal);
                Extensions.CalculateRandomScale(web, false, 6f, 6f);

                item.Despawn();
            }
        }
    }
}
