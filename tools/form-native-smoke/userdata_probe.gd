extends SceneTree

# 本阶段只预检原生目录. 不加载游戏, 不启动进程, 不创建窗口或网络连接.
const ROUND_ROOT := "G:/omp works/.tmp/form-playable-20260928-01a0e7ad"
const PROJECT_NAME := "FormNativeUserdataProbe"
var _report: FileAccess
var _errors: Array[String] = []


func _initialize() -> void:
	var options := _parse_arguments()
	var expected_root := _g_path(str(options.get("--expected-appdata-root", "")))
	var report_path := _g_path(str(options.get("--report", "")))
	var user_data := _g_path(OS.get_user_data_dir())
	var observed := {
		"engine": Engine.get_version_info(),
		"user_data_dir": OS.get_user_data_dir(),
		"display_server": DisplayServer.get_name(),
		"audio_driver": AudioServer.get_driver_name(),
		"executable": OS.get_executable_path(),
		"expected_appdata_root": expected_root,
		"report": report_path,
		"appdata": OS.get_environment("APPDATA"),
		"localappdata": OS.get_environment("LOCALAPPDATA"),
		"temp": OS.get_environment("TEMP"),
		"tmp": OS.get_environment("TMP"),
		"project_name": ProjectSettings.get_setting("application/config/name", ""),
		"file_logging_enabled": ProjectSettings.get_setting_with_override("debug/file_logging/enable_file_logging"),
	}

	# 故意比仅检查盘符更严格: 所有隔离根必须在本轮 .tmp 下.
	# 在拒绝不安全参数时不创建任何报告, 只向已由调用者重定向的 stdout 输出 JSON.
	if not _under(report_path, ROUND_ROOT) or not report_path.ends_with(".jsonl"):
		_errors.append("报告必须是本轮 G: .tmp 下的新 .jsonl 文件.")
		_finish_without_report(observed, 2)
		return
	var report_problem := _directory_problem(report_path.get_base_dir())
	if not report_problem.is_empty():
		_errors.append(report_problem)
		_finish_without_report(observed, 2)
		return
	var parent := DirAccess.open(report_path.get_base_dir())
	if parent == null:
		_errors.append("报告父目录无法打开.")
		_finish_without_report(observed, 3)
		return
	var leaf := report_path.get_file()
	if parent.is_link(leaf) or parent.file_exists(leaf) or parent.dir_exists(leaf):
		_errors.append("报告目标已存在或为链接, 拒绝覆盖旧证据.")
		_finish_without_report(observed, 2)
		return
	_report = FileAccess.open(report_path, FileAccess.WRITE)
	if _report == null:
		_errors.append("报告打开失败: " + str(FileAccess.get_open_error()))
		_finish_without_report(observed, 3)
		return
	if not _emit("observed", "OBSERVED", observed):
		_close_and_quit(3)
		return

	if OS.get_name() != "Windows":
		_errors.append("预检仅适用于当前 Windows 二进制.")
	if not _under(expected_root, ROUND_ROOT):
		_errors.append("调用者必须明确指定本轮 .tmp 下的隔离 APPDATA 根, 不接受共享 G:/appdata.")
	else:
		_add_directory_problem(expected_root)
	if _g_path(OS.get_environment("APPDATA")).to_lower() != expected_root.to_lower() or expected_root.is_empty():
		_errors.append("进程 APPDATA 与显式指定的隔离根不一致.")
	if not _under(user_data, expected_root):
		_errors.append("OS.get_user_data_dir() 未落在指定的隔离 APPDATA 根内.")
	elif _under(expected_root, ROUND_ROOT):
		# 引擎可能还没有创建最后的用户目录; 不为验证路径而向 user:// 写文件.
		var user_problem := _directory_problem(user_data, true)
		if not user_problem.is_empty():
			_errors.append(user_problem)
	for variable in ["LOCALAPPDATA", "TEMP", "TMP"]:
		var isolated := _g_path(OS.get_environment(variable))
		if not _under(isolated, ROUND_ROOT):
			_errors.append(variable + " 必须指向本轮 G: 隔离目录.")
		else:
			_add_directory_problem(isolated)
	if DisplayServer.get_name().to_lower() != "headless":
		_errors.append("实际 DisplayServer 不是 headless.")
	if AudioServer.get_driver_name().to_lower() != "dummy":
		_errors.append("实际音频驱动不是 Dummy.")
	if observed["file_logging_enabled"] != false:
		_errors.append("文件日志没有关闭.")
	if observed["project_name"] != PROJECT_NAME:
		_errors.append("加载的不是独立预检项目.")
	if ProjectSettings.get_setting("application/config/use_custom_user_dir", false) != true:
		_errors.append("预检项目没有使用独立命名的用户目录.")
	if ProjectSettings.get_setting("application/config/custom_user_dir_name", "") != PROJECT_NAME:
		_errors.append("用户目录名不是预检专用名称.")
	if str(ProjectSettings.get_setting("application/run/main_scene", "")) != "":
		_errors.append("预检项目不允许 main_scene.")
	for property in ProjectSettings.get_property_list():
		if str(property["name"]).begins_with("autoload/"):
			_errors.append("预检项目不允许 autoload: " + str(property["name"]))

	var exit_code := 0 if _errors.is_empty() else 2
	var status := "PASS" if exit_code == 0 else "FAIL"
	if not _emit("result", status, {"observed": observed, "exit_code": exit_code}):
		exit_code = 3
	_close_and_quit(exit_code)


func _parse_arguments() -> Dictionary:
	var result: Dictionary = {}
	for argument in OS.get_cmdline_user_args():
		var equals := argument.find("=")
		if equals < 1:
			_errors.append("预检参数必须使用 --key=value, 并放在 -- 之后.")
			continue
		var key := argument.substr(0, equals)
		if key != "--expected-appdata-root" and key != "--report":
			_errors.append("未知预检参数: " + key)
			continue
		if result.has(key):
			_errors.append("重复预检参数: " + key)
			continue
		result[key] = argument.substr(equals + 1)
	for required in ["--expected-appdata-root", "--report"]:
		if not result.has(required):
			_errors.append("缺少预检参数: " + required)
	return result


func _g_path(raw: String) -> String:
	var value := raw.replace("\\", "/").trim_suffix("/")
	if value.length() <= 3 or value.substr(0, 3).to_lower() != "g:/":
		return ""
	var components := value.substr(3).split("/", true)
	for component in components:
		if component.is_empty() or component == "." or component == ".." or component.ends_with(".") or component.ends_with(" "):
			return ""
		for index in range(component.length()):
			if component.unicode_at(index) < 32:
				return ""
		for forbidden in ["<", ">", ":", "\"", "|", "?", "*", "~"]:
			if component.contains(forbidden):
				return ""
		var stem := component.get_slice(".", 0).to_lower()
		if stem in ["con", "prn", "aux", "nul"]:
			return ""
		if stem.length() == 4 and (stem.begins_with("com") or stem.begins_with("lpt")) and stem.substr(3) in ["1", "2", "3", "4", "5", "6", "7", "8", "9"]:
			return ""
	return "G:/" + "/".join(components)


func _under(child: String, parent: String) -> bool:
	return not child.is_empty() and not parent.is_empty() and child.to_lower().begins_with(parent.to_lower() + "/")


func _directory_problem(directory: String, allow_missing_tail: bool = false) -> String:
	# Godot 4.5.1 DirAccess.is_link 包括 Windows junction 和其它 reparse point.
	var current := "G:/"
	for component in directory.substr(3).split("/", false):
		var parent := DirAccess.open(current)
		if parent == null:
			return "无法检查目录: " + current
		if parent.is_link(component):
			return "隔离路径含链接或 reparse point: " + current.path_join(component)
		if parent.file_exists(component):
			return "隔离路径的目录段是文件: " + current.path_join(component)
		if not parent.dir_exists(component):
			if allow_missing_tail:
				return ""
			return "隔离目录尚未由调用者创建: " + current.path_join(component)
		current = current.path_join(component)
	return ""


func _add_directory_problem(directory: String) -> void:
	var problem := _directory_problem(directory)
	if not problem.is_empty():
		_errors.append(problem)


func _record(event: String, status: String, actual: Dictionary) -> Dictionary:
	return {
		"schema": 1, "event": event, "status": status,
		"time_utc": Time.get_datetime_string_from_system(true),
		"pid": OS.get_process_id(), "actual": actual, "errors": _errors.duplicate(),
	}


func _emit(event: String, status: String, actual: Dictionary) -> bool:
	var line := JSON.stringify(_record(event, status, actual))
	_report.store_line(line)
	_report.flush()
	if _report.get_error() != OK:
		_errors.append("报告写入或刷新失败: " + str(_report.get_error()))
		print(JSON.stringify(_record("result", "FAIL", {"exit_code": 3, "report_persisted": false})))
		return false
	print(line)
	return true


func _finish_without_report(actual: Dictionary, exit_code: int) -> void:
	actual["exit_code"] = exit_code
	actual["report_persisted"] = false
	print(JSON.stringify(_record("result", "FAIL", actual)))
	quit(exit_code)


func _close_and_quit(exit_code: int) -> void:
	_report.close()
	_report = null
	quit(exit_code)
