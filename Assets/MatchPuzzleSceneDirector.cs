using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchPuzzleSceneDirector : MonoBehaviour
{
    // 変更可能なゲームルール
    // フィールドサイズ横
    [SerializeField] int fieldWidth;
    // フィールドサイズ縦
    [SerializeField] int fieldHeight;
    // ◯個揃うと消える
    [SerializeField] int matchColorCount;
    // 加算スコア
    [SerializeField] int deleteScore;
    // 制限時間
    [SerializeField] float gameTimer;

    // 背景
    [SerializeField] SpriteRenderer field;
    // タイルのプレハブ
    [SerializeField] TileController prefabTile;
    // 時間
    [SerializeField] TextMeshProUGUI textGameTimer;
    // スコア
    [SerializeField] TextMeshProUGUI textGameScore;
    // コンボカウント
    [SerializeField] TextMeshProUGUI textCombo;
    // ゲーム終了画面
    [SerializeField] GameObject panelResult;
    // ゲーム終了画面のスコア
    [SerializeField] TextMeshProUGUI textResultScore;
    // サウンド
    [SerializeField] AudioClip seDelete;

    // フィールドデータ
    TileController[,] fieldTiles;

    // ゲームモード
    enum GameMode
    {
        WaitFall,
        Delete,
        Fall,
        Spawn,
        Touch,
        WaitSwap,
        WaitBackSwap,
    }

    GameMode gameMode;

    // 交換するタイルのインデックス
    Vector2Int swapIndexA;
    Vector2Int swapIndexB;
    // 押した座標
    Vector2 touchDownPoint;
    // 押したフラグ
    bool isTouchDown;

    // スコア
    int gameScore;

    // コンボ数
    int comboCount;

    // サウンド
    AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        // サウンド
        audioSource = GetComponent<AudioSource>();

        // フィールドデータ作成
        fieldTiles = new TileController[fieldWidth, fieldHeight];
        // ゲーム画面のフィールド作成
        field.transform.localScale = new Vector2(fieldWidth, fieldHeight);

        // スコアとコンボを初期化
        gameScore = 0;
        comboCount = 0;

        // コンボテキスト非表示
        UpdateTextCombo();

        // リザルト画面非表示
        panelResult.SetActive(false);

        // タイル生成
        SpawnTiles();

        // 最初のモード
        gameMode = GameMode.WaitFall;
    }

    // Update is called once per frame
    void Update()
    {
        // タイムリミット
        gameTimer -= Time.deltaTime;

        // ゲームオーバー
        if (gameTimer < 0)
        {
            GameResult();
            gameTimer = -1;
        }

        // タイマー表示更新
        textGameTimer.text = "" + (int)(gameTimer + 1);

        // 全タイルが落ちきるのを待つモード
        if (gameMode == GameMode.WaitFall)
        {
            WaitFallMode();
        }
        // 削除モード
        else if (gameMode == GameMode.Delete)
        {
            DeleteMode();
        }
        // タイル落下モード
        else if (gameMode == GameMode.Fall)
        {
            FallMode();
        }
        // タイル生成モード
        else if (gameMode == GameMode.Spawn)
        {
            SpawnMode();
        }
        // タッチできるモード
        else if (gameMode == GameMode.Touch)
        {
            TouchMode();
        }
        // タイル移動を待つモード
        else if (gameMode == GameMode.WaitSwap)
        {
            WaitSwapMode();
        }
        // タイルが元の状態に戻るのを待つモード
        else if (gameMode == GameMode.WaitBackSwap)
        {
            WaitBackSwapMode();
        }
    }

    // インデックス座標をワールド座標に変換
    public Vector2 IndexToWorldPosition(int x, int y)
    {
        Vector2 position = new Vector2();

        position.x = x + 0.5f - fieldWidth / 2.0f;
        position.y = y + 0.5f - fieldHeight / 2.0f;

        return position;
    }

    // タイル生成
    TileController SpawnTile(int x, int y)
    {
        // インデックスからワールド座標に変換
        Vector2 position = IndexToWorldPosition(x, y);
        // タイル生成
        TileController tile = Instantiate(prefabTile, position, Quaternion.identity);

        return tile;
    }

    // 配列外かチェック
    public bool IsOutOfRange(int x, int y)
    {
        if (x < 0 || fieldWidth - 1 < x || y < 0 || fieldHeight - 1 < y)
        {
            return true;
        }

        return false;
    }

    // フィールドデータのゲット（配列外、データが無い場合はnull）
    public TileController GetFieldTile(int x, int y)
    {
        if (IsOutOfRange(x, y)) return null;
        return fieldTiles[x, y];
    }

    // フィールドデータのセット（デフォルトはクリア）
    public void SetFieldTile(int x, int y, TileController tile = null)
    {
        if (IsOutOfRange(x, y)) return;
        // タイルをセット
        fieldTiles[x, y] = tile;

    }

    // 足りないタイルを生成
    void SpawnTiles()
    {
        for (int x = 0; x < fieldWidth; x++)
        {
            // 生成したカウント分上に積んでいく
            int emptyCount = 0;
            for (int y = 0; y < fieldHeight; y++)
            {
                // データがある場合何もしない
                if (GetFieldTile(x, y)) continue;

                // フィールド外から生成
                TileController tile = SpawnTile(x, fieldHeight + emptyCount);

                // ターゲット位置へ落下
                tile.GravityFall(IndexToWorldPosition(x, y));

                // 内部データ更新
                SetFieldTile(x, y, tile);

                // 次の生成位置を一段上に上げる
                emptyCount++;
            }
        }
    }

    // 全タイルが落ちきるのを待つモード
    void WaitFallMode()
    {
        // 移動終了を待つ
        if (!IsEndMoveTiles()) return;

        // 次のモード
        gameMode = GameMode.Delete;
    }

    // 削除モード
    void DeleteMode()
    {
        // デフォルトでタッチモードへ
        gameMode = GameMode.Touch;

        // 削除可能タイル
        List<Vector2Int> deleteTiles = GetDeleteTiles();

        // 削除可能なら落ちるモードへ
        if (deleteTiles.Count > 0)
        {
            // タイル削除
            DeleteTiles(deleteTiles);
            // 次のモード
            gameMode = GameMode.Fall;
        }
        // タッチモード遷移前
        else
        {
            // 移動可能なタイル
            List<Vector2Int> movableTiles = GetMovableTiles();

            // 移動可能なタイルがない
            if (movableTiles.Count < 1)
            {
                // リストをクリアして使い回す
                deleteTiles.Clear();

                // 全タイルのインデックスをリストに追加
                foreach (var tile in fieldTiles)
                {
                    Vector2Int index = WorldToIndexPosition(tile.transform.position);
                    deleteTiles.Add(index);
                }

                // 全タイルを削除
                DeleteTiles(deleteTiles);

                // 次のモード
                gameMode = GameMode.Fall;
            }
        }
    }

    // タイル落下モード
    void FallMode()
    {
        // 全タイルを落下させる
        FallTiles();

        // 次のモードへ
        gameMode = GameMode.Spawn;
    }

    // タイル生成モード
    void SpawnMode()
    {
        // 空いているフィールドにタイルを生成
        SpawnTiles();

        // 次のモード
        gameMode = GameMode.WaitFall;
    }

    // タッチできるモード
    void TouchMode()
    {
        // タッチした時
        if (Input.GetMouseButtonDown(0))
        {
            // スクリーン座標からワールド座標に変換
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // レイを飛ばす
            RaycastHit2D hit2d = Physics2D.Raycast(worldPoint, Vector2.zero);

            // 当たり判定があった場合
            if (hit2d)
            {
                // 触ったタイルのインデックス
                swapIndexA = WorldToIndexPosition(hit2d.transform.position);
                // 押下された座標
                touchDownPoint = worldPoint;
                // タッチ開始フラグ
                isTouchDown = true;
            }
        }
        // タッチが離された時
        else if (Input.GetMouseButtonUp(0))
        {
            // タッチフラグチェック
            if (!isTouchDown) return;

            // スクリーン座標からワールド座標に変換
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // 離した方向
            Vector2 vec = worldPoint - touchDownPoint;
            // 移動先のインデックス
            Vector2Int indexDirection = Vector2Int.zero;

            // 縦軸
            if (Mathf.Abs(vec.x) < Mathf.Abs(vec.y))
            {
                indexDirection = Vector2Int.up;
                if (vec.y < 0)
                {
                    indexDirection = Vector2Int.down;
                }
            }
            // 横軸
            else if (Mathf.Abs(vec.y) < Mathf.Abs(vec.x))
            {
                indexDirection = Vector2Int.right;
                if (vec.x < 0)
                {
                    indexDirection = Vector2Int.left;
                }
            }

            //交換するタイルのインデックス
            swapIndexB = swapIndexA + indexDirection;

            // タイル入れ替え
            SwapTiles(swapIndexA, swapIndexB);

            // タッチフラグをリセット
            isTouchDown = false;

            // コンボリセット
            comboCount = 0;
            UpdateTextCombo();

            // 次のモード
            gameMode = GameMode.WaitSwap;
        }
    }

    // タイル移動を待つモード
    void WaitSwapMode()
    {
        // 移動が終了していない
        if (!IsEndMoveTiles()) return;

        // 削除できるタイルを取得
        List<Vector2Int> deleteTiles = GetDeleteTiles();

        // 削除モードへ
        if (deleteTiles.Count > 0)
        {
            gameMode = GameMode.Delete;
        }
        // 削除できなければ元の状態に戻す
        else
        {
            SwapTiles(swapIndexA, swapIndexB);
            // 次のモード
            gameMode = GameMode.WaitBackSwap;
        }
    }

    // タイルが元の状態に戻るのを待つモード
    void WaitBackSwapMode()
    {
        // 移動が終了していない
        if (!IsEndMoveTiles()) return;

        // 次のモード
        gameMode = GameMode.Touch;
    }

    // ワールド座標をインデックス座標に変換
    Vector2Int WorldToIndexPosition(Vector2 position)
    {
        Vector2Int index = new Vector2Int();

        float x = position.x - 0.5f + fieldWidth / 2.0f;
        float y = position.y - 0.5f + fieldHeight / 2.0f;

        index.x = (int)x;
        index.y = (int)y;

        return index;
    }

    // 2つのタイルデータを入れ替える（視覚的なポジションの移動はしない）
    bool SwapTileDatas(Vector2Int indexA, Vector2Int indexB)
    {
        // 配列外
        if (IsOutOfRange(indexA.x, indexA.y) || IsOutOfRange(indexB.x, indexB.y)) return false;
        // 同じ場所
        if (indexA == indexB) return false;

        // データを入れ替える
        TileController tmpTile = fieldTiles[indexA.x, indexA.y];
        fieldTiles[indexA.x, indexA.y] = fieldTiles[indexB.x, indexB.y];
        fieldTiles[indexB.x, indexB.y] = tmpTile;

        return true;
    }

    // 2つのタイルデータとポジションを入れ替える
    void SwapTiles(Vector2Int indexA, Vector2Int indexB)
    {
        // 内部データを交換
        bool isSwapTileDatas = SwapTileDatas(indexA, indexB);

        //交換できなかった（配列外　or 同じ場所）
        if (!isSwapTileDatas) return;

        // 視覚的に移動
        fieldTiles[indexA.x, indexA.y].SwapMove(fieldTiles[indexB.x, indexB.y].transform.position);
        fieldTiles[indexB.x, indexB.y].SwapMove(fieldTiles[indexA.x, indexA.y].transform.position);
    }

    // 全てのタイルの移動が完了したか
    bool IsEndMoveTiles()
    {
        // 2次元配列内の全要素にアクセス
        foreach (var item in fieldTiles)
        {
            // データがない
            if (!item) continue;

            //コンポーネントが有効
            if (item.enabled) return false;
        }
        return true;
    }

    // 指定された方向のマッチしたタイルを返す
    List<Vector2Int> GetMatchTiles(Vector2Int index, List<Vector2Int> directions)
    {
        // チェック済のマッチタイル
        List<Vector2Int> matchTiles = new List<Vector2Int>();

        // 開始位置を追加
        matchTiles.Add(index);

        // このカラーと同じカラーを探す
        int mainColor = GetFieldTile(index.x, index.y).ColorType;

        // 全方向分調べる
        foreach (var dir in directions)
        {
            // 開始位置
            Vector2Int checkIndex = index + dir;

            // まだ追加されていなければループする
            while (!matchTiles.Contains(checkIndex))
            {
                // タイルデータ取得
                TileController tile = GetFieldTile(checkIndex.x, checkIndex.y);

                // 配列オーバー or データなし
                if (!tile) break;

                // 違う色
                if (mainColor != tile.ColorType) break;

                // 同じ色なので追加
                matchTiles.Add(checkIndex);

                // 調べる位置を進める
                checkIndex += dir;
            }
        }

        return matchTiles;
    }

    // 新しいアイテムのみ追加
    void AddNewItems(List<Vector2Int> targetList, List<Vector2Int> items)
    {
        foreach (var item in items)
        {
            if (targetList.Contains(item)) continue;
            targetList.Add(item);
        }
    }

    // 全体から削除可能なタイルを返す
    List<Vector2Int> GetDeleteTiles()
    {
        // 削除対象タイル
        List<Vector2Int> deleteTiles = new List<Vector2Int>();

        // 全体を調べる
        foreach (var tile in fieldTiles)
        {
            // データなし
            if (!tile) continue;

            // ここに中心に調べる
            Vector2Int index = WorldToIndexPosition(tile.transform.position);

            // 左右方向
            List<Vector2Int> directions = new List<Vector2Int>()
            {
                Vector2Int.left,
                Vector2Int.right
            };

            // 同じ色のタイルを取得
            List<Vector2Int> matchTiles = GetMatchTiles(index, directions);

            // マッチ数に達していたら追加
            if (matchColorCount <= matchTiles.Count)
            {
                // 被りのないリストを作成
                AddNewItems(deleteTiles, matchTiles);
            }

            // 上下方向
            directions = new List<Vector2Int>()
            {
                Vector2Int.up,
                Vector2Int.down
            };

            // 同じ色のタイルを取得
            matchTiles = GetMatchTiles(index, directions);

            // マッチ数に達していたら追加
            if (matchColorCount <= matchTiles.Count)
            {
                // 被りのないリストを作成
                AddNewItems(deleteTiles, matchTiles);
            }

        }

        return deleteTiles;
    }

    // タイル削除
    void DeleteTiles(List<Vector2Int> deleteTiles)
    {
        foreach (var item in deleteTiles)
        {
            // データなし
            TileController tile = GetFieldTile(item.x, item.y);
            if (!tile) continue;

            // 削除
            tile.Delete();
            // 内部データクリア
            SetFieldTile(item.x, item.y);
        }

        // スコアとコンボの計算

        // コンボ数更新
        comboCount++;
        UpdateTextCombo();

        // 基本スコア
        int baseScore = deleteTiles.Count * deleteScore;
        // コンボスコア
        int comboScore = comboCount * deleteScore;
        // スコア更新
        gameScore += baseScore + comboScore;
        textGameScore.text = "" + gameScore;

        // SE再生
        audioSource.PlayOneShot(seDelete);
    }

    // 指定されたタイルの一番下の空いているタイルのy座標を返す
    int GetBottomY(int x, int y)
    {
        // 返却するy座標
        int bottomY = -1;

        //　一番下のyを探す
        for (int checkY = y - 1; 0 <= checkY; checkY--)
        {
            // 配列外
            if (IsOutOfRange(x, checkY)) continue;

            // 空いていたら返却する値を更新
            if (!GetFieldTile(x, checkY))
            {
                bottomY = checkY;
            }
        }

        return bottomY;
    }

    // 全てのタイルを落下させる
    void FallTiles()
    {
        foreach (var tile in fieldTiles)
        {
            // タイルデータなし
            if (!tile) continue;

            // 落下させるタイルのインデックス
            Vector2Int indexA = WorldToIndexPosition(tile.transform.position);

            // 落下先のy座標
            int bottomY = GetBottomY(indexA.x, indexA.y);

            // 落下先がない
            if (bottomY == -1) continue;

            // 落下させる場所
            Vector2Int indexB = new Vector2Int(indexA.x, bottomY);

            // 内部データ更新
            SwapTileDatas(indexA, indexB);

            // 視覚的に落下させる
            tile.GravityFall(IndexToWorldPosition(indexB.x, indexB.y));
        }
    }

    // コンビ数表示
    void UpdateTextCombo()
    {
        // 表示するテキスト
        string text = "" + comboCount + "Combo!";

        // 1以下は表示しない
        if (comboCount < 2)
        {
            text = "";
        }

        textCombo.text = text;
    }

    // 削除を伴う移動ができるタイルのリストを返す
    List<Vector2Int> GetMovableTiles()
    {
        // 消せるタイルのインデックス
        List<Vector2Int> movableTiles = new List<Vector2Int>();

        // 全タイルを調べる
        foreach (var tile in fieldTiles)
        {
            // このタイルから調べる
            Vector2Int indexA = WorldToIndexPosition(tile.transform.position);

            // 上下左右に動かす
            List<Vector2Int> directions = new List<Vector2Int>()
            {
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };

            foreach (var dir in directions)
            {
                // 交換先のインデックス
                Vector2Int indexB = indexA + dir;

                // データだけを入れ替える
                SwapTileDatas(indexA, indexB);

                // 1つでも消せる
                if (GetDeleteTiles().Count > 0)
                {
                    if (!movableTiles.Contains(indexA))
                    {
                        movableTiles.Add(indexA);
                    }
                }

                // 元に戻す
                SwapTileDatas(indexA, indexB);
            }
        }

        return movableTiles;
    }

    // ゲーム終了
    void GameResult()
    {
        // リザルトパネル表示
        textResultScore.text = "" + gameScore;
        panelResult.SetActive(true);

        // Updateを停止
        enabled = false;
    }

    // リトライのボタンが押された時の処理
    public void OnClickRetryButton()
    {
        SceneManager.LoadScene("MatchPuzzleScene");
    }
}
