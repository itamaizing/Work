using System.Collections;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;

public class SkillRenderer : NetworkBehaviour
{
    [SerializeField] private Character hero;
    [SerializeField] private DrawCircle _circle;
    [SerializeField] private CircleArea _areaPref;
    [SerializeField] private SphereArea _damageZonePref;
    [SerializeField] private AbilityLineRenderer _line;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private Color _colorForAllies = Color.green;
    [SerializeField] private Color _colorForEnemies = Color.red;
    [SerializeField] private Color _colorForEnd;
    [SerializeField] private Color _colorForStart;

    private bool _isOverrideClosestTarget = false;
    //private SphereArea _tempDamageZone;
    private CircleArea _tempArea;
    private float _lineStartLength;
   // private float _lineEndLength;
    private float _boxLength;
    private float _boxWidth;
    private float _circleRadius;
    private BoxArea _lineStartImage;
    //private BoxArea _lineEndImage;

    private Coroutine _drawLineCoroutine;
    private Coroutine _drawAreaCoroutine;
    private Coroutine _drawClosestTargetCoroutine;
    private Coroutine _drawRadiusCoroutine;
    private Coroutine _dynamicRadiusColorCoroutine;

    //public SphereArea TempDamageZone => _tempDamageZone;
    public CircleArea TempDamageZone => _tempArea;
    public bool IsOverrideClosestTarget
    {
        get => _isOverrideClosestTarget;
        set
        {
            _isOverrideClosestTarget = value;
            if (_isOverrideClosestTarget) StopDrawClosestTarget();
        }
    }

    private Character _tempTarget;

    [Command]
    public void CmdDrawDamageZone(Vector3 position, float radius, Damage damage, GameObject player)
    {
        RpcDrawDamageZone(position, radius, damage, player);
    }

    [ClientRpc]
    public void RpcDrawDamageZone(Vector3 position, float radius, Damage damage, GameObject player)
    {
		/* _tempDamageZone = Instantiate(_damageZonePref, position, Quaternion.identity);
		 _tempDamageZone.SetSize(radius, damage);

		 Color zoneColor = player.layer == LayerMask.NameToLayer("Allies") ? _colorForAllies : _colorForEnemies;
		 _tempDamageZone.SetColor(zoneColor);*/

		_tempArea = Instantiate(_areaPref, position, Quaternion.identity);
		_tempArea.SetSize(radius, damage);

		Color zoneColor = player.layer == LayerMask.NameToLayer("Allies") ? _colorForAllies : _colorForEnemies;
		_tempArea.SetColor(zoneColor);
	}

    [Command]
    public void CmdStopDrawDamageZone()
    {
        RpsStopDrawDamageZone();
    }

    [ClientRpc]
    public void RpsStopDrawDamageZone()
    {
		/* if (_tempDamageZone != null)
		 {
			 Destroy(_tempDamageZone.gameObject);
		 }*/
		if (_tempArea != null)
		{
			Destroy(_tempArea.gameObject);
		}
	}

    public void DrawRadius(float radius)
    {
        _circle.Draw(radius);
    }

    public void StopDrawRadius()
    {
        _circle.Clear();
        if (_drawRadiusCoroutine != null)
        {
            StopCoroutine(_drawRadiusCoroutine);
            _drawRadiusCoroutine = null;
        }
    }

    public void DrawRadiusColor(float radius, Color color) 
    {
        _circle.SetColor(color);
        _circle.Draw(radius);
    }

    public void SetColor(Color color)
    {
        _circle.SetColor(color);
    }

    public void DrawArea(float radius, Damage damage, LayerMask layerMask, CircleArea area = null)
    {
        if (area == null)
            area = _areaPref;

        _circleRadius = radius;
        _drawAreaCoroutine = StartCoroutine(DrawAreaJob(radius, damage, layerMask, area));
    }

    public void StopDrawArea()
    {
        if (_drawAreaCoroutine != null)
        {
            StopCoroutine(_drawAreaCoroutine);
            _drawAreaCoroutine = null;
        }

        if (_tempArea != null)
        {
            Destroy(_tempArea.gameObject);
            _tempArea = null;
        }
    }

    public void DrawLine(float length, float width, Damage damage, LayerMask layerMask, AbilityLineRenderer line = null)
    {
        if (line == null)
            line = _line;
        _boxWidth = length;
        _boxWidth = width;
        _drawLineCoroutine = StartCoroutine(DrawLineJob(length, width, damage, layerMask, line));
    }

    public void StopDrawLine()
    {
        if (_drawLineCoroutine != null)
        {
            StopCoroutine(_drawLineCoroutine);
            _drawLineCoroutine = null;
        }

        if (_lineStartImage != null)
        {
            Destroy(_lineStartImage.gameObject);
            _lineStartImage = null;
        }
    }

    public void DrawClosestTarget(float radius, LayerMask TargetsLayers, Character player)
    {
        if (_isOverrideClosestTarget) return;
        _drawClosestTargetCoroutine = StartCoroutine(DrawClosestTargetJob(radius, TargetsLayers, player));
    }

    public void StopDrawClosestTarget()
    {
		if (_drawClosestTargetCoroutine != null)
        {
            StopCoroutine(_drawClosestTargetCoroutine);
            _drawClosestTargetCoroutine = null;
        }    

        if(_tempTarget != null)
        {
            _tempTarget.SelectedCircle.SwitchClostestTarget(false);
            _tempTarget = null;
        }
	}

    public void SetSizeBox(float width, float lenght)
    {
        _boxWidth = width;
        _boxLength = lenght;
    }

    public void SetRadiusArea(float radiusArea)
    {
        _circleRadius = radiusArea;
    }

    public void StartDynamicRadiusColor(float radius)
    {
        if (_dynamicRadiusColorCoroutine != null)
            StopCoroutine(_dynamicRadiusColorCoroutine);

        _dynamicRadiusColorCoroutine = StartCoroutine(DynamicRadiusColorJob(radius));
    }

    public void StopDynamicRadiusColor()
    {
        if (_dynamicRadiusColorCoroutine != null)
        {
            StopCoroutine(_dynamicRadiusColorCoroutine);
            _dynamicRadiusColorCoroutine = null;
        }
    }


    private void RotateAtMouse(Transform transform)
    {
		Vector3 worldPosition = Vector3.zero;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, _layerMask))
		{
			worldPosition = hit.point;
		}

		//Vector3 dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
		Vector3 dir = worldPosition - gameObject.transform.position;
		float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(90, - angle + 90, 0);
    }

    private IEnumerator DrawRadiusJob(float radius)
    {
        while (true)
        {
            bool hasEnemyInRadius = false;

            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

            foreach (var collider in colliders)
            {
                if (collider.TryGetComponent<Character>(out Character character) && character != hero)
                {
                    hasEnemyInRadius = true;
                    break;
                }
            }

            _circle.SetColor(hasEnemyInRadius ? _colorForAllies : _colorForEnemies);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator DrawLineJob(float length, float width, Damage damage,  LayerMask layerMask, AbilityLineRenderer line)
    {
        _boxLength = length;
        _boxWidth = width;
        _lineStartImage = Instantiate(line.Start, transform);
		_lineStartImage.SetSize(_boxWidth, _boxLength, damage);
		//  _lineEndImage = Instantiate(line.End, transform);

		_lineStartImage.SetColor(_colorForStart);
      //  _lineEndImage.SetColor(_colorForEnd);

        while (true)
        {
            if (_lineStartImage == null) yield break;

            RotateAtMouse(_lineStartImage.transform);
            _lineStartImage.SetSize(_boxWidth, _boxLength, damage);
            yield return null;
        }
    }

    private IEnumerator DrawAreaJob(float radius, Damage damage, LayerMask layerMask, CircleArea areaPref)
    {
		Vector3 worldPosition = Vector3.zero;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			worldPosition = hit.point;
		}

		//Vector3 mouse = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x,0 , Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
		Vector3 mouse = new Vector3(worldPosition.x, 0 , worldPosition.z);

        _tempArea = Instantiate(areaPref, mouse, Quaternion.Euler(90, 0, 0));
        _tempArea.SetSize(_circleRadius, damage);

        while (true)
        {
            if (_tempArea == null) yield break;
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out hit, _layerMask))
			{
				worldPosition = hit.point;
			}
			// mouse = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x,0 , Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
			mouse = new Vector3(worldPosition.x, 0 , worldPosition.z);
            _tempArea.transform.position = mouse;
            yield return null;
        }
    }

    private IEnumerator DrawClosestTargetJob(float radius, LayerMask TargetsLayers, Character player)
    {
        while (true)
        {
            if (IsOverrideClosestTarget) yield return null;

            List<Character> targets = new List<Character>();
            Collider[] collider = Physics.OverlapSphere(transform.position, radius + 500);

            foreach (var item in collider)
            {
                if (collider.Length > 0 && item.transform.TryGetComponent<Character>(out Character enemy))
                {
                    if (enemy == player)
                    {
                        continue;
                    }
                    targets.Add(enemy);
                }
            }
            targets = targets.OrderBy(character => Vector3.Distance(character.transform.position, gameObject.transform.position)).ToList();
            if (targets.Count > 0)
            {
				foreach (var target in targets)
				{
					if (Vector3.Distance(target.transform.position, transform.position) <= radius)
					{
						target.SelectedCircle.SwitchStroke(true);
						target.SelectedCircle.SetColorTarget(Color.green);
					}
					else
					{
						target.SelectedCircle.SwitchStroke(false);
					}
				}

				if (_tempTarget != null)
                {
                    if (Vector3.Distance(_tempTarget.transform.position, transform.position) > Vector3.Distance(targets[0].transform.position, transform.position))
                    {
                        _tempTarget.SelectedCircle.SwitchClostestTarget(false);
                        _tempTarget = targets[0];
                    }
                }

                _tempTarget = targets[0];
                _tempTarget.SelectedCircle.SwitchClostestTarget(true);

                if (Vector3.Distance(_tempTarget.transform.position, transform.position) <= radius)
                {
                    _tempTarget.SelectedCircle.SetColorTarget(Color.green);
                }
                else
                {
                    _tempTarget.SelectedCircle.SetColorTarget(Color.red);
                }
            }
			yield return null;
		}
		//yield return null;
	}

    private IEnumerator DynamicRadiusColorJob(float Radius)
    {
        while (true)
        {
            if (_tempArea != null && _circle != null)
            {
                float distance = Vector3.Distance(_tempArea.transform.position, transform.position);

                if (distance <= Radius)
                {
                    _circle.SetColor(_colorForAllies);
                }
                else
                {
                    _circle.SetColor(_colorForEnemies);
                }

                _circle.Draw(Radius);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }
}
