# -*- coding: utf-8 -*-
"""生成 Installer/Direct 三套语言包。zh_cn 为基准，简->繁映射生成 zh_tw/zh_hk。
输出扁平 key（如 common.ok: "..."），是合法 YAML 且与 YamlReader 直接对应。"""
import os, io

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# 简体 -> 繁体 字级映射（覆盖本项目用词）
S2T = {
 '请':'請','击':'擊','选':'選','择':'擇','资':'資','稳':'穩','复':'複','数':'數','据':'據',
 '显':'顯','动':'動','页':'頁','说':'說','网':'網','络':'絡','盘':'碟','载':'載','压':'壓',
 '装':'裝','启':'啟','检':'檢','测':'測','间':'間','总':'總','夹':'夾','创':'創','统':'統',
 '历':'歷','区':'區','发':'發','预':'預','览':'覽','戏':'戲','单':'單','误':'誤','认':'認',
 '试':'試','败':'敗','换':'換','暂':'暫','于':'於','脑':'腦','制':'製','携':'攜','质':'質',
 '备':'備','险':'險','结':'結','现':'現','删':'刪','占':'佔','输':'輸','续':'續','径':'徑',
 '议':'議','盖':'蓋','从':'從','开':'開','验':'驗','线':'線','较':'較','无':'無','务':'務',
 '处':'處','应':'應','当':'當','独':'獨','关':'關','闭':'閉','护':'護','释':'釋','卫':'衛',
 '机':'機','构':'構','杂':'雜','优':'優','级':'級','仅':'僅','后':'後','余':'餘','联':'聯',
 '补':'補','风':'風','恶':'惡','桩':'樁','简':'簡','体':'體','译':'譯','语':'語','环':'環',
 '变':'變','读':'讀','写':'寫','边':'邊','过':'過','还':'還','这':'這','进':'進','时':'時',
 '长':'長','为':'為','个':'個','们':'們','国':'國','问':'問','题':'題','该':'該','在':'在',
 '会':'會','运':'運','触':'觸','坏':'壞','毁':'毀','链':'鏈','阶':'階','级':'級','别':'別',
 '势':'勢','样':'樣','办':'辦','增':'增','减':'減','并':'並','与非':'與非','与':'與','参':'參',
 '术':'術','帮':'幫','码':'碼','显':'顯','隐':'隱','藏':'藏','轻':'輕','重':'重','资源':'資源',
 '点':'點','击':'擊','击':'擊','始':'始','结':'結','够':'夠','进':'進','还':'還','边':'邊',
 '项':'項','状':'狀態'.replace('狀態','狀'),'朗':'朗','稳':'穩','联':'聯','机':'機','灵':'靈',
 '级':'級','细':'細','难':'難','单':'單','双':'雙','适':'適合'.replace('適合','適'),'铺':'鋪',
 '里':'裡','几':'幾','个':'個','为':'為','么':'麼','样':'樣','条':'條','着':'著',
}

WORDS = [
 ('文件系统','檔案系統'), ('文件夹','資料夾'), ('文件','檔案'), ('网络','網路'),
 ('下载','下載'), ('安装','安裝'), ('启动','啟動'), ('检测','偵測'), ('磁盘','磁碟'),
 ('盘符','磁碟機代號'), ('U盘','USB 隨身碟'), ('时间','時間'), ('空间','空間'),
 ('剩余空间','剩餘空間'), ('浏览','瀏覽'), ('打开','開啟'), ('关闭','關閉'),
 ('校验','校驗'), ('失败','失敗'), ('重试','重試'), ('刷新','重新整理'),
 ('加载','載入'), ('选择','選擇'), ('资源','資源'), ('用户','使用者'),
 ('电脑','電腦'), ('制作','製作'), ('便携','攜帶'), ('介质','媒體'),
 ('备份','備份'), ('危险','危險'), ('结构','結構'), ('删除','刪除'),
 ('占用','佔用'), ('其它','其他'), ('拔出','拔出'), ('目录','目錄'),
 ('数据','資料'), ('清除','清除'), ('格式化','格式化'), ('继续','繼續'),
 ('路径','路徑'), ('建议','建議'), ('覆盖','覆蓋'), ('立即','立即'),
 ('手动','手動'), ('完整','完整'), ('线上','線上'), ('直装','直裝'),
 ('无需','無需'), ('条件','條件'), ('简体中文','簡體中文'), ('繁体中文','繁體中文'),
 ('安装目录','安裝目錄'), ('启动程序','啟動程式'), ('快捷方式','捷徑'),
 ('脚本','指令碼'), ('整合包','整合包'), ('版本','版本'), ('历史','歷史'),
 ('社区','社群'), ('开发','開發'), ('稳定版','穩定版'), ('预览版','預覽版'),
 ('同文件','同檔案'), ('压缩包','壓縮包'), ('非法路径','非法路徑'),
 ('获取','取得'), ('缓存','快取'), ('缓存','快取'), ('列表','清單'), ('列表','清單'),
 ('所以','所以'), ('网络','網路'), ('连接','連線'), ('默认','預設'), ('默认','預設'),
 ('点击','點擊'), ('检测','偵測'), ('制作','製作'), ('整合包','整合包'),
 ('目标','目標'), ('位置','位置'), ('字节','位元組'), ('校验','校驗'),
 ('选项','選項'), ('安装','安裝'), ('程序','程式'), ('信息','資訊'),
]

def conv(s):
    for a, b in sorted(WORDS, key=lambda x: -len(x[0])):
        s = s.replace(a, b)
    return ''.join(S2T.get(ch, ch) for ch in s)

COMMON = {
 'common.ok':'确定', 'common.cancel':'取消', 'common.back':'上一步', 'common.next':'下一步',
 'common.finish':'完成', 'common.retry':'重试', 'common.refresh':'刷新', 'common.select':'选择',
 'common.hint':'提示', 'common.error':'错误', 'common.warning':'警告', 'common.tip':'提示',
 'common.unknown':'未知', 'common.none':'（无）', 'common.occupied':'已被占用',
 'wizard.step':'步骤 {0} / {1}', 'wizard.defaultPageTitle':'步骤', 'wizard.required':'请先完成当前步骤的必填项。',
 'wizard.exitConfirm':'确定要退出向导吗？', 'wizard.exitCaption':'确认退出',
 'wizard.errorCaption':'错误', 'wizard.tipCaption':'提示',
 'welcome.start':'请点击「下一步」开始。',
 'res.title':'选择资源', 'res.head.official':'官方整合包', 'res.head.historical':'历史版本',
 'res.head.community':'社区开发版本', 'res.head.games':'单个游戏',
 'res.channel.stable':'稳定版', 'res.channel.preview':'预览版', 'res.channel.dev':'开发版',
 'res.refresh':'刷新列表', 'res.select':'选择', 'res.loading':'正在加载资源列表…',
 'res.loadingWait':'正在加载资源列表，请稍候…', 'res.loadFailed':'加载失败',
 'res.loadFailedMsg':'资源列表加载失败：{0}\n可点击右上角「刷新列表」重试。',
 'res.offlineCache':'（离线缓存）', 'res.empty':'资源列表为空{0}',
 'res.loadedCount':'共加载资源 {0} 项{1}', 'res.emptyClick':'暂无资源，请点击「刷新列表」。',
 'res.categoryEmpty':'该分类下暂无资源。', 'res.noDesc':'（暂无描述）',
 'res.unknownSize':'未知大小', 'res.unknownTime':'未知时间', 'res.updatedOn':'更新于 {0}',
 'res.noResSlot':'该通道暂无资源', 'res.noResSlotDesc':'请切换通道或稍后刷新',
 'progress.ready':'准备中…', 'log.expand':'展开日志 ▾', 'log.collapse':'收起日志 ▴',
 'api.fetchFailed':'获取在线资源失败：{0}，尝试读取本地缓存。',
 'api.emptyResponse':'服务器返回空内容',
 'api.fetchOk':'资源列表获取成功（第 {0} 次尝试）。',
 'api.retryFail':'第 {0}/{1} 次请求失败：{2}',
 'api.allFail':'资源列表获取失败，已重试 {0} 次：{1}',
 'api.jsonEmpty':'JSON 顶层为空', 'api.parseFail':'资源列表 JSON 解析失败：{0}',
 'api.cacheWriteFail':'写入资源缓存失败：{0}', 'api.cacheReadFail':'读取资源缓存失败：{0}',
 'dl.emptyUrl':'资源下载地址为空', 'dl.prepare':'准备下载（第 {0} 次）',
 'dl.start':'开始下载：{0}（第 {1} 次）', 'dl.done':'下载完成：{0}',
 'dl.verified':'下载完成，校验通过', 'dl.complete':'下载完成',
 'dl.retryFail':'下载失败（第 {0}/{1} 次）：{2}', 'dl.failAll':'下载失败：{0}',
 'icon.cacheFail':'图标写缓存失败：{0}', 'icon.loadFail':'图标加载失败 {0}：{1}',
 'checksum.unsupported':'不支持的校验算法：{0}',
 'checksum.mismatch':'校验和不匹配：期望 {0}，实际 {1}', 'checksum.fail':'校验失败：{0}',
 'package.illegalPath':'压缩包包含非法路径：{0}',
 'usb.removable':'可移动磁盘', 'usb.device':'USB 设备', 'usb.free':'{0} 可用',
}

INSTALLER = {
 'app.title':'jftw Go Installer — U 盘启动盘制作器',
 'welcome.title':'欢迎使用 jftw Go Installer',
 'welcome.heading':'jftw Go · U 盘启动盘制作器',
 'welcome.body':'本工具用于在高配置电脑上，把选好的 jftw Go 资源写入 U 盘，制作成可携带的启动介质。\n\n使用前提：\n  • 一台运行 Windows 的电脑；\n  • 一个可正常读写的 U 盘（部署过程不会格式化或清除现有文件）；\n  • 良好的网络连接用于下载所选资源。\n\n注意：本程序不是 jftw Go 的普通安装程序。若你只想在线安装并直接运行，\n请使用 jftw Go Direct。',
 'usb.title':'选择 U 盘', 'usb.detected':'检测到以下可移动磁盘：', 'usb.rescan':'重新检测',
 'usb.notSelected':'尚未选择 U 盘。',
 'usb.hint':'提示：本工具不会格式化 U 盘，仅在盘中写入 jftw_Go_Assets 目录与启动入口。',
 'usb.notDetected':'未检测到可移动磁盘。请插入 U 盘后点击「重新检测」。',
 'usb.pleaseSelect':'请在上方列表中选择一个 U 盘。',
 'usb.detail':'盘符：{0}\n卷标：{1}\n文件系统：{2}\n总容量：{3}    可用空间：{4}',
 'deploy.title':'确认部署', 'deploy.heading':'确认资源部署信息',
 'deploy.structure':'将在 U 盘上创建如下结构（不会格式化或删除现有文件）：\n  jftw_Go_Assets\\version\\<整合包>\\   ← 存放所选整合包\n  jftw Go.lnk  /  jftw Go.bat            ← 根目录启动入口\n\n多个整合包可以共存：每个整合包在 version 下拥有独立文件夹，\n类似 Minecraft 的 .minecraft\\version 结构。',
 'deploy.check':'我已了解上述部署方式，确认继续。',
 'deploy.summary':'将部署资源：{0}\n目标 U 盘：{1}（{2}，可用 {3}）\n整合包文件夹：jftw_Go_Assets\\version\\{4}',
 'deploy.occupied':'注意：U 盘上已存在 jftw_Go_Assets 文件夹。将向其 version 目录追加/更新该整合包，现有其它文件不会被删除。',
 'write.title':'正在制作 U 盘启动盘', 'write.retry':'重试', 'write.cancel':'取消',
 'write.userCancel':'用户取消了制作流程。', 'write.canceled':'已取消。',
 'write.noSelect':'资源或 U 盘未选择，无法开始制作。',
 'write.usbGone':'所选 U 盘已不在位（可能被拔出），请返回重新检测。',
 'write.noSpace':'U 盘可用空间不足，无法写入该资源。',
 'write.downloading':'开始下载资源…', 'write.deploying':'正在部署资源到 U 盘…',
 'write.downloadingFmt':'正在下载… {0}', 'write.deployFail':'部署失败：{0}',
 'write.shortcutOk':'已创建根目录快捷方式与启动脚本。', 'write.done':'制作完成。{0}',
 'write.fail':'制作失败：{0}', 'write.failedPrefix':'失败：{0}', 'write.unknown':'未知错误',
 'done.title':'制作完成', 'done.heading':'U 盘资源部署完成',
 'done.tip':'你可以安全拔出 U 盘，或返回重新制作另一块。',
 'done.packageDir':'\n整合包目录：jftw_Go_Assets\\version\\{0}\n',
 'done.shortcutOk':'根目录已生成 jftw Go.lnk 快捷方式与 jftw Go.bat 启动脚本。\n',
 'done.shortcutBad':'启动入口创建不完整，请检查 U 盘根目录。\n',
 'done.body':'资源：{0}\n目标 U 盘：{1}\n{2}\nU 盘上的其它文件未被改动，可安全拔出。',
 'writer.deployTarget':'部署目标目录：{0}', 'writer.deployDone':'部署完成，启动程序：{0}',
 'writer.noLauncher':'未在整合包中找到 {0}，资源已解压但未创建快捷方式。',
 'writer.noWsh':'系统未提供 WScript.Shell', 'writer.desc':'启动 jftw Go',
 'writer.lnkOk':'已创建快捷方式：{0}', 'writer.lnkFail':'创建快捷方式失败：{0}',
 'writer.batRemark':'rem jftw Go 启动脚本（相对路径，盘符变化仍可用）',
 'writer.batOk':'已创建启动脚本：{0}', 'writer.batFail':'创建 bat 失败：{0}',
 'writer.noDrive':'盘符为空',
}

DIRECT = {
 'app.title':'jftw Go Direct — 在线直装',
 'welcome.title':'欢迎使用 jftw Go Direct',
 'welcome.heading':'jftw Go · 在线直装（无需 U 盘）',
 'welcome.body':'本工具适用于没有 U 盘、但网络条件较好的电脑：\n在线下载 jftw Go 资源，解压安装到你指定的目录，并可直接启动。\n\n使用前提：\n  • 可正常访问资源站点的网络；\n  • 一个可写的安装目录（建议非系统盘）。\n\n注意：本程序需要联网下载资源，不做 U 盘制作。\n若你要制作可携带的 U 盘启动盘，请使用 jftw Go Installer。',
 'options.title':'选择安装位置', 'options.heading':'安装位置', 'options.browse':'浏览…',
 'options.hint':'建议安装到非系统盘或用户目录下的独立文件夹。安装会覆盖目标目录中的同名文件。',
 'options.launch':'安装完成后立即启动 jftw Go', 'options.folderDesc':'选择 jftw Go 的安装目录',
 'dl.title':'下载并安装', 'dl.retry':'重试', 'dl.cancel':'取消',
 'dl.userCancel':'用户取消了安装流程。', 'dl.canceled':'已取消。',
 'dl.noSelect':'未选择资源，无法安装。', 'dl.noDir':'未指定安装目录。',
 'dl.downloading':'开始下载资源…', 'dl.downloadingFmt':'正在下载… {0}',
 'dl.installing':'正在安装到目标目录…', 'dl.extracting':'正在解压安装…',
 'dl.copied':'已复制资源文件。', 'dl.done':'安装完成。', 'dl.fail':'安装失败：{0}',
 'dl.failedPrefix':'失败：{0}', 'dl.noLauncher':'未找到可启动程序，已跳过启动。',
 'dl.launched':'已启动：{0}', 'dl.launchFail':'启动失败：{0}',
 'done.title':'安装完成', 'done.heading':'jftw Go 已安装完成',
 'done.openDir':'打开安装目录',
 'done.body':'资源：{0}\n安装目录：{1}\n\n{2}',
 'done.launched':'已尝试启动 jftw Go。', 'done.notLaunched':'未自动启动，可从安装目录手动运行。',
}

def write_yaml(path, d):
    with io.open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(u'# jftw Go 语言包（key: value；{0} 为占位符，\\n 为换行）\n')
        for k in sorted(d.keys()):
            val = d[k].replace('\\', '\\\\').replace('"', '\\"').replace('\n', '\\n')
            f.write(u'%s: "%s"\n' % (k, val))

for app, extra in [('Installer', INSTALLER), ('Direct', DIRECT)]:
    base = dict(COMMON); base.update(extra)
    for lang, d in [('zh_cn', base), ('zh_tw', {k:conv(v) for k,v in base.items()}),
                    ('zh_hk', {k:conv(v) for k,v in base.items()})]:
        write_yaml(os.path.join(ROOT, app, 'lang', lang + '.yaml'), d)
        print('wrote', app, lang, len(d))
