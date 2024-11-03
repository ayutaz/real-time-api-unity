# OpenAI Realtime API Unity プロジェクト

このプロジェクトは、Unity上でOpenAIのRealtime APIを使用し、マイク入力から音声を取得してAIとリアルタイムに対話するアプリケーションの実装例です。

<!-- TOC -->
* [OpenAI Realtime API Unity プロジェクト](#openai-realtime-api-unity-プロジェクト)
* [概要](#概要)
  * [目的](#目的)
  * [主な機能:](#主な機能)
* [動作環境](#動作環境)
* [必要なパッケージとライブラリ](#必要なパッケージとライブラリ)
* [使い方](#使い方)
* [ライセンス](#ライセンス)
<!-- TOC -->

# 概要
## 目的
Unityでマイクから音声入力を取得し、OpenAIのRealtime APIを介してAIとリアルタイムに音声対話を行い、そのレスポンスを音声で再生する。
## 主な機能:
* マイクからの音声入力取得
* 取得した音声データのフォーマット変換と送信
* OpenAI Realtime APIとのWebSocket通信
* APIからの音声レスポンスの受信と再生

# 動作環境
Unity バージョン: 2022.3.42f1

プラットフォーム:
* 開発環境: Mac (Unity Editor)
* 対応プラットフォーム: Mac、Android

# 必要なパッケージとライブラリ
* [UniTask](https://github.com/Cysharp/UniTask): 非同期処理を簡潔に扱うためのUnity向けライブラリ
* [WebSocket Client for Unity](https://github.com/mikerochip/unity-websocket): UnityでWebSocket通信を行うためのライブラリ
* Newtonsoft.Json: JSONデータのシリアライズとデシリアライズを行うライブラリ

# 使い方
1. `SampleScene`を開きます。
2. `OpenAIRealtimeAPI`オブジェクトの`OpenAI Realtime API`コンポーネントの`API Key`フィールドに、OpenAIのAPIキーを入力します。
3. Unityエディタで再生ボタンをクリックしてシーンを実行します。
4. マイクに向かって話しかけます。
5. OpenAI Realtime APIを介して、AIからの音声レスポンスが再生されます。

# ライセンス
[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)