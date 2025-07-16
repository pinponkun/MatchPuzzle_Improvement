using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TileController : MonoBehaviour
{
    // タイルの画像リスト
    [SerializeField] List<Sprite> matchColors;

    // このタイルのカラー
    public int ColorType;

    // 移動速度
    const float MoveSpeed = 3.5f;
    // 目標地点
    Vector2 targetPosition;
    // 目標距離
    float targetDistance;
    // 物理挙動
    Rigidbody2D rigidbody2d;

    // コンポーネントが有効になった時に呼ばれる
    private void Awake()
    {
        // Rigidbody2Dコンポーネントを取得
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // カラーをランダムに設定
        ColorType = Random.Range(0, matchColors.Count);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = matchColors[ColorType];
    }

    // Update is called once per frame
    void Update()
    {
        // 移動量（デフォルトは下方向への移動量）
        float moveDistance = rigidbody2d.velocity.y * Time.deltaTime;

        // 交換用の移動
        if (rigidbody2d.gravityScale < 1)
        {
            // MoveSpeedを使って移動量を設定
            moveDistance = MoveSpeed * Time.deltaTime;

            //移動
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveDistance);
        }

        // 残りの距離
        targetDistance -= Mathf.Abs(moveDistance);

        // 目標距離を消化
        if (targetDistance < 0)
        {
            // 移動を止めてピッタリの位置に補正する
            SnapToTargetPosition();
        }
    }

    // タイル移動
    public void SwapMove(Vector2 position)
    {
        // 目標位置
        targetPosition = position;
        // 目標移動量
        targetDistance = Vector2.Distance(transform.position, position);
        // このコンポーネントのUpdateに入るようにする
        enabled = true;
    }

    // 自然落下
    public void GravityFall(Vector2 position)
    {
        // 設定する項目は同じ
        SwapMove(position);

        // 重力オン
        rigidbody2d.gravityScale = MoveSpeed;
    }

    // 動きを止めて位置を補正する
    void SnapToTargetPosition()
    {
        // 重力をリセット
        rigidbody2d.gravityScale = 0;
        // 移動量をリセット
        rigidbody2d.velocity = Vector2.zero;
        // ポジションをターゲットピッタリに合わせる
        transform.position = targetPosition;

        // このコンポーネントのUpdateに入らないようにする
        enabled = false;
    }

    // 消える時の演出
    public void Delete()
    {
        // 表示を後ろに移動
        GetComponent<SpriteRenderer>().sortingOrder = -10;
        // タイル同士ぶつかり合うようにする
        GetComponent<BoxCollider2D>().isTrigger = false;

        // ランダムで力を加える
        Vector2 force = new Vector2(Random.Range(-500.0f, 500.0f), Random.Range(-500.0f, 1000.0f));
        rigidbody2d.AddForce(force);

        // 重力
        rigidbody2d.gravityScale = MoveSpeed;

        // 軽めの質量に変更
        rigidbody2d.mass = 0.6f;

        // 2秒後に削除
        Destroy(gameObject, 2);
    }
}
