#!/usr/bin/env python3

# Copyright (c) Meta Platforms, Inc. and its affiliates.
# This source code is licensed under the MIT license found in the
# LICENSE file in the root directory of this source tree.

import argparse
from http.server import HTTPServer, SimpleHTTPRequestHandler
import http.server
import os

class ContentHTTPHandler(http.server.SimpleHTTPRequestHandler):
    """
    BaseHTTPRequestHandler for serving Unity addressable bundles.
    """
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=os.getcwd(), **kwargs)

    def end_headers(self):
        # Set the CORS header for every response.
        self.send_header('Access-Control-Allow-Origin', '*')
        SimpleHTTPRequestHandler.end_headers(self)

def start_server(path: str, hostname: str, port: int) -> None:
    """Start the server."""
    os.chdir(path)
    server = HTTPServer((hostname, port), ContentHTTPHandler)
    print(f"Serving bundles at: 'http://{hostname}:{port}'.")
    server.serve_forever()

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        prog="Unity Content Server",
        description=(
            """
            Simple HTTP server that serves addressable content for Unity.
            Designed for local emulation of content provision services like S3.
            Unlike a normal HTTP server, it sets the CORS header required by browsers.
            """
        ),
    )
    parser.add_argument(
        "--path",
        type=str,
        default="ServerData/",
        help="Path to the content to serve. Typically, the folder is named 'ServerData'.",
    )
    parser.add_argument(
        "--hostname",
        type=str,
        default="localhost",
        help="Server hostname.",
    )
    parser.add_argument(
        "--port",
        type=int,
        default=9999,
        help="Server port. For emulating S3, use 80 for HTTP and 443 for HTTPS.",
    )

    args = parser.parse_args()
    start_server(args.path, args.hostname, args.port)
