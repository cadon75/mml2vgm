﻿from mml2vgmIDE import ScriptInfo
from mml2vgmIDE import Mml2vgmInfo
from System.IO import Directory
from System.IO import Path
from System.IO import File

class Mml2vgmScript:

    #スクリプトのタイトル
    #複数のタイトルを持つ場合は|をデリミタとして列挙する。(|はタイトル文字として使用できない)
    #run呼び出し時のindexが0から順に割り当てられる
    def title(self):
        return r"Add index|Commit"

    #このスクリプトはどこから実行されることを想定しているかを指定する
    #複数のタイトルを持つ場合はその分だけ|をデリミタとして列挙する。
    # FromMenu メインウィンドウのメニューストリップ、スクリプトから実行されることを想定
    # FromTreeViewContextMenu ツリービューのコンテキストメニューから実行されることを想定
    def scriptType(self):
        return r"FromTreeViewContextMenu|FromTreeViewContextMenu"

    #このスクリプトがサポートするファイル拡張子を列挙する
    def supportFileExt(self):
        return r".*|.*"

    def run(self, Mml2vgmInfo, index):
        
        #設定値の読み込み
        Mml2vgmInfo.loadSetting()

        #初回のみ(設定値が無いときのみ)git.exeの場所をユーザーに問い合わせ、設定値として保存する
        gt = Mml2vgmInfo.getSettingValue("gitpath")
        if gt is None:
            gt = Mml2vgmInfo.fileSelect("git.exeを選択してください(この選択内容は設定値として保存され次回からの問い合わせはありません)")
            if not Mml2vgmInfo.confirm("git.exeの場所は以下でよろしいですか\r\n" + gt):
                return None
            Mml2vgmInfo.setSettingValue("gitpath",gt)
            Mml2vgmInfo.saveSetting()
        
        #念のため
        if gt is None or gt == "":
            Mml2vgmInfo.msg("git.exeを指定してください")
            return None

        si = ScriptInfo()

        commitMsg = ""
        if index == 1:
            #git コミット
            commitMsg = Mml2vgmInfo.inputBox("コミット時のコメントを入力してください")
            if commitMsg == "":
                return si

        #ファイル情報の整理
        for fnf in Mml2vgmInfo.fileNamesFull:
            wp = Path.GetDirectoryName(fnf)
            Directory.SetCurrentDirectory(wp)

            if index == 0:
                #git ステージング
                args = "add " + fnf
                ret = Mml2vgmInfo.runCommand(gt, args, True)
                if ret != "":
                    Mml2vgmInfo.msg(ret)
            else:
                #git コミット
                args = "commit -m\"" + commitMsg + "\""
                ret = Mml2vgmInfo.runCommand(gt, args, True)
                if ret != "":
                    Mml2vgmInfo.msg(ret)

        return si

