using System.Collections;
using System.Diagnostics;
using DunGen;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace WendigoEnemy {
    class WendigoEnemyAI : EnemyAI
    {
        //#pragma warning disable 0649
        //#pragma warning restore 0649
        enum State {
            SearchingForPlayer, //TO DO: replace with blind dog arrgro method?
            ChasingPlayer, 
            Observe,    //TO DO: add attach to ceiling in wait
            MaulPlayer, //TO DO: add attach to player
        }

        [Conditional("DEBUG")]
        void LogIfDebugBuild(string text) {
            Plugin.Logger.LogInfo(text);
        }

        public override void Start() {
            base.Start();
            LogIfDebugBuild("Wendigo Spawned");
            DoAnimationClientRpc("Walk");
            SwitchToBehaviourClientRpc((int)State.SearchingForPlayer); //TO DO: change to idle or ceiling cling?

            StartSearch(transform.position);
        }

        public override void Update() {
            base.Update();
            timeSinceHittingPlayer += Time.deltaTime;
            if(isEnemyDead){
                return;
            }
            this.ScreetchOnInterval();
        }

        public override void DoAIInterval() {
            
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) {
                return;
            }

            switch(currentBehaviourStateIndex) {
                case (int)State.SearchingForPlayer:
                    agent.speed = 3f;

                    if (FoundClosestPlayerInRange(25f, 3f)){
                        LogIfDebugBuild("Start Target Player");
                        StopSearch(currentSearch);
                        SwitchToBehaviourClientRpc((int)State.ChasingPlayer);
                        DoAnimationClientRpc("Run");
                        creatureAnimator.SetBool("isChasing", true);
                    }
                    
                    break;

                case (int)State.ChasingPlayer:                    // Keep targeting closest player, unless they are over 20 units away and we can't see them.
                    agent.speed = 13f;
                    if (!TargetClosestPlayerInAnyCase() || (Vector3.Distance(transform.position, targetPlayer.transform.position) > 20 && !CheckLineOfSightForPosition(targetPlayer.transform.position))){
                        LogIfDebugBuild("Stop Target Player");
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForPlayer);
                        creatureAnimator.SetBool("isChasing", false);
                        return;
                    }
                    DamagePlayersPerSecond();
                    SetDestinationToPosition(targetPlayer.transform.position);
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f, true);       

                    break;
                case (int)State.Observe:
                    agent.speed = 0f;
                    LogIfDebugBuild("Observe");
                    creatureAnimator.SetBool("isSearching", false);

                    break;
                case (int)State.MaulPlayer:                    // Attach to player, maul for 30 damage, release, play delaying animation
                    agent.speed = 0f;
                                      
                    LogIfDebugBuild("Start Mauling target");
                    SwitchToBehaviourClientRpc((int)State.ChasingPlayer);

                    break;                   
                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }
        }
        
        bool FoundClosestPlayerInRange(float range, float senseRange) {
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: true);
            if(targetPlayer == null){                // Couldn't see a player, so we check if a player is in sensing distance instead
                TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: false);
                range = senseRange;
            }
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) < range;
        }
        private IEnumerator killPlayerAnimation(int playerObject){//baboon
            PlayerControllerB killedPlayer = StartOfRound.Instance.allPlayerScripts[playerObject];
            this.creatureAnimator.ResetTrigger("KillAnimation");
		    this.creatureAnimator.SetTrigger("KillAnimation");
		    this.creatureVoice.PlayOneShot(this.enemyType.audioClips[4]);
            yield return null;
        }
        public void DamagePlayersPerSecond(){       //add if below 20 health maulPlayer
            foreach(PlayerControllerB player in StartOfRound.Instance.allPlayerScripts){
                if (Vector3.Distance(transform.position, player.transform.position) < 3f){
                    player.DamagePlayer(7);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    //LogIfDebugBuild("Wendigo dealt damage");
                    if(this.timeSinceHittingPlayer < 4){
                        return;
                    }
                    this.timeSinceHittingPlayer = 0f;
                    this.ScreetchClientRpc(2,3,4);
                }
            }
        }
        bool TargetClosestPlayerInAnyCase() {
            mostOptimalDistance = 2000f;
            targetPlayer = null;
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
                if (tempDist < mostOptimalDistance)
                {
                    mostOptimalDistance = tempDist;
                    targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
                }
            }
            if(targetPlayer == null) return false;
            return true;
        }

        public override void HitEnemy(int force = 25, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            if(isEnemyDead){
                return;
            }
            enemyHP -= force;
            if (IsOwner) {
                if (enemyHP <= 0 && !isEnemyDead) {                  
                    StopCoroutine(searchCoroutine); // need to stop our search coroutine, because the game does not do that by default
                    KillEnemyOnOwnerClient();
                }
            }
        }
        private void ScreetchOnInterval(){
            this.screetchInterval -= Time.deltaTime;
            if (screetchInterval <= 0f){
                this.ScreetchClientRpc(0,1,4);
                this.screetchInterval = Random.Range(2f, 15f);
            }
        }
        [ClientRpc]
	    public void ScreetchClientRpc(int a, int b, int c){
            int num = Random.Range(0,100);
            if(num < 33){
                this.creatureVoice.PlayOneShot(this.enemyType.audioClips[a]);
                //LogIfDebugBuild("screetch 0!");
            }
            if(num < 66 && num > 33){
                this.creatureVoice.PlayOneShot(this.enemyType.audioClips[b]);
                //LogIfDebugBuild("screetch 1!");
            }
            if(num > 66){
                this.creatureVoice.PlayOneShot(this.enemyType.audioClips[c]);
                //LogIfDebugBuild("screetch 4!");
            }
        }
        [ClientRpc]
        public void DoAnimationClientRpc(string animationName) {
            LogIfDebugBuild($"Animation: {animationName}");
            creatureAnimator.SetTrigger(animationName);
        }
        /*
        [ServerRpc(RequireOwnership = false)]
        public void MaulPlayerServerRpc(int playerId){
            NetworkManager networkManager = base.NetworkManager;
            if(networkManager == null || !networkManager.IsListening){
                return;
            }
            if(this.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost)){
            ServerRpcParams serverRpcParams;
			FastBufferWriter writer = base.__beginSendServerRpc(2965927486U, serverRpcParams, RpcDelivery.Reliable);
			BytePacker.WriteValueBitPacked(writer, playerId);
			base.__endSendServerRpc(ref writer, 2965927486U, serverRpcParams, RpcDelivery.Reliable);
            }
        }
        */
        public float screetchInterval = 0f;
        public float timeSinceHittingPlayer = 0f;
        private DeadBodyInfo killAnimationBody;
    }
    
}