#!/usr/bin/env python3
import argparse
import os
import re
import subprocess
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path

RESULT_RE = re.compile(r"\[ OK ] Program result: (.+)$", re.MULTILINE)


def run_once(command):
    start = time.perf_counter()
    proc = subprocess.run(command, capture_output=True, text=True)
    end = time.perf_counter()
    output = (proc.stdout or "") + "\n" + (proc.stderr or "")
    match = RESULT_RE.findall(output)
    if match:
        result = match[-1].strip()
    else:
        result = str(proc.returncode)
    return {
        "exit_code": proc.returncode,
        "duration_ms": (end - start) * 1000.0,
        "result": result,
        "output": output,
    }


def run_test(project, path, jit_threshold):
    base_cmd = [
        "dotnet",
        "run",
        "--project",
        project,
        "--",
        str(path),
        "--mem",
        "10",
    ]
    no_jit = run_once(base_cmd)
    jit_cmd = base_cmd + ["--jit", str(jit_threshold)]
    with_jit = run_once(jit_cmd)

    same_result = no_jit["exit_code"] == 0 and with_jit["exit_code"] == 0
    same_output = no_jit["output"].strip() == with_jit["output"].strip()

    return {
        "path": str(path),
        "no_jit": no_jit,
        "jit": with_jit,
        "passed": same_result and same_output,
        "same_output": same_output,
    }


def collect_programs(root_dir):
    return sorted(Path(root_dir).rglob("*.sk"))


def format_duration(ms):
    return f"{ms:.2f} ms"


def main():
    parser = argparse.ArgumentParser(
        description="Run Skipper regression programs with/without JIT and compare results.")
    parser.add_argument("--root", default="regressions",
                        help="Root folder with .sk programs (relative to Skipper project folder)")
    parser.add_argument("--project", default="Skipper.csproj", help="Path to Skipper.csproj")
    parser.add_argument("--jit-threshold", type=int, default=30, help="JIT hot threshold")
    parser.add_argument("--jobs", type=int, default=max(os.cpu_count() or 2, 2), help="Parallel jobs")
    args = parser.parse_args()

    base_dir = Path(__file__).resolve().parents[1]
    root_dir = Path(args.root)
    if not root_dir.is_absolute():
        root_dir = base_dir / root_dir

    project_path = Path(args.project)
    if not project_path.is_absolute():
        project_path = base_dir / project_path
    if not project_path.exists():
        alt_path = base_dir / "Skipper.csproj"
        if alt_path.exists():
            project_path = alt_path

    if not root_dir.exists():
        print(f"[FAIL] Programs folder not found: {root_dir}")
        return 2

    programs = collect_programs(root_dir)
    if not programs:
        print(f"[FAIL] No .sk files found in: {root_dir}")
        return 2

    results = []
    with ThreadPoolExecutor(max_workers=args.jobs) as executor:
        futures = [executor.submit(run_test, str(project_path), path, args.jit_threshold) for path in programs]
        for future in as_completed(futures):
            results.append(future.result())

    results.sort(key=lambda r: r["path"])
    passed = sum(1 for r in results if r["passed"])
    failed = len(results) - passed

    print("\nRegression report")
    print("=================")
    print(f"Programs: {len(results)}")
    print(f"Passed:   {passed}")
    print(f"Failed:   {failed}\n")

    for r in results:
        status = "OK" if r["passed"] else "FAIL"
        print(f"[{status}] {r['path']}")
        print(f"  no-jit: {format_duration(r['no_jit']['duration_ms'])} -> {r['no_jit']['result']}")
        print(f"  jit:    {format_duration(r['jit']['duration_ms'])} -> {r['jit']['result']}")
        print(f"  output match: {r['same_output']}")
        if not r["passed"]:
            if r["no_jit"]["exit_code"] != 0:
                print("  no-jit error:")
                print(r["no_jit"]["output"].strip())
            if r["jit"]["exit_code"] != 0:
                print("  jit error:")
                print(r["jit"]["output"].strip())
            if r["no_jit"]["exit_code"] == 0 and r["jit"]["exit_code"] == 0 and not r["same_output"]:
                print("  no-jit output:")
                print(r["no_jit"]["output"].strip())
                print("  jit output:")
                print(r["jit"]["output"].strip())
        print("")
        # print("  --- no-jit output ---")
        # print(r["no_jit"]["output"].rstrip())
        # print("  --- jit output ---")
        # print(r["jit"]["output"].rstrip())

    return 1 if failed > 0 else 0


if __name__ == "__main__":
    raise SystemExit(main())
