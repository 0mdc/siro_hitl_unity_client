from multiprocessing import Process
from time import sleep

import argparse
import os

import http_content_server
import http_webgl_server

def content_server_main(build_path: str):
    http_content_server.start_server(build_path, "127.0.0.1", 9999)

def unity_server_main(build_path: str):
    http_webgl_server.start_server(build_path, "127.0.0.1", 3333)

def main(build_path: str):
    content_server_process = Process(target=content_server_main, args=[build_path])
    unity_server_process = Process(target=unity_server_main, args=[build_path])

    content_server_process.start()
    unity_server_process.start()

    print("Example link:")
    print("http://127.0.0.1:3333/index.html?server_hostname=127.0.0.1&server_port=8888&asset_hostname=localhost&asset_port=9999&asset_path=ServerData&episodes=4-8")

    while content_server_process.is_alive() and unity_server_process.is_alive():
        sleep(1.0)

    print("Terminating...")
    if content_server_process.is_alive():
        content_server_process.terminate()
        content_server_process.join()
    if unity_server_process.is_alive():
        unity_server_process.terminate()
        unity_server_process.join()
    print("Terminated...")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        prog="TODO",
        description=(
            """
            TODO
            """
        ),
    )
    parser.add_argument(
        "path",
        type=str,
        nargs="?",
        help="Path to the Unity WebGL build (where 'index.html' is located). 'ServerData' must be located in this folder.",
    )

    args = parser.parse_args()
    main(args.path)