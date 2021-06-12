using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StateMachineNamespace;
using UnityEngine.EventSystems;

[System.Serializable]
public class TargetSelectState : StateMachine.State
{
    private BattleManager battleManager;
    public Battlefield battlefield;

    private TurnData turnData;

    private List<Combatant> availableTargets = new List<Combatant>();
    private Combatant selectedTarget;

    private GameObject targetingHitboxObject;

    private void Start()
    {
        battleManager = GetComponentInParent<BattleManager>();
    }

    public override void OnEnter()
    {
        turnData = battleManager.turnData;

        //zoom out camera to entire battlefield
        battlefield.cameraManager.ZoomOut();

        SetTargetableCombatants();
    }

    private void SetTargetableCombatants()
    {
        //add allies and/or enemies to list of possible targets
        if(turnData.action.targetFriendly)
        {
            availableTargets.AddRange(battleManager.allyParty.combatants);
        }
        if(turnData.action.targetHostile)
        {
            availableTargets.AddRange(battleManager.enemyParty.combatants);
        }
        foreach(Combatant target in availableTargets)
        {
            target.targetIcon.ToggleButton(true);
        }
        SetInitialTarget();
    }

    private void SetInitialTarget()
    {   
        //set default target
        if(availableTargets.Contains(turnData.combatant))
        {
            SetTarget(turnData.combatant);
        }
        else
        {
            SetTarget(GetNearestTarget());
        }
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectedTarget.gameObject.GetComponentInChildren<TargetIcon>().gameObject);
    }

    public void SetTarget(Combatant newTarget)
    {
        GridManager gridManager = battlefield.gridManager;

        if(selectedTarget != null)
        {
            selectedTarget.tile.ToggleAOE(false);
        }

        selectedTarget = newTarget;
        selectedTarget.tile.ToggleAOE(true);

        if(turnData.action.actionType == ActionType.GenericAttack)
        {
            Action meleeAttack = turnData.combatant.battleStats.meleeAttack;
            Action rangedAttack = turnData.combatant.battleStats.rangedAttack;

            if(meleeAttack && meleeAttack.range >= gridManager.GetMoveCost(turnData.combatant.tile, selectedTarget.tile))
            {
                turnData.action = meleeAttack;
            }
            else if(rangedAttack)
            {
                turnData.action = rangedAttack;
            }
        }
    }

    private Combatant GetNearestTarget()
    {
        Combatant closestTarget = null;
        float smallestDistance = Mathf.Infinity;
        
        foreach(Combatant target in availableTargets)
        {
            float thisDistance = Vector3.Distance(turnData.combatant.transform.position, target.transform.position);

            if(thisDistance < smallestDistance)
            {
                smallestDistance = thisDistance;
                closestTarget = target;
            }
        }
        return closestTarget;
    }

    public override void StateUpdate()
    {
        if(Input.GetButtonDown("Select"))
        {
            turnData.targets.Add(selectedTarget);
            turnData.targetedTile = selectedTarget.tile;
            nextState = "ExecuteAction";
        }
        else if(Input.GetButtonDown("Cancel"))
        {
            battleManager.turnData.action = null;
            nextState = "BattleMenu";
        }
    }

    public override void StateFixedUpdate()
    {

    }

    public override string CheckConditions()
    {
        return nextState;
    }

    public override void OnExit()
    {
        availableTargets.Clear();
        if(selectedTarget != null)
        {
            selectedTarget = null;
            selectedTarget.tile.ToggleAOE(false);
        }
    }

    public void OnTargetChange(Combatant newTarget)
    {
        SetTarget(newTarget);
    }
}
