lic = '''
ScriptsManager, Administrador de scripts
Copyright (C) 2020 Erick Mora

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.

erickfernandomoraramirez@gmail.com
erickmoradev@gmail.com
https://dev.moradev.dev/myportfolio
'''

from _thread import start_new_thread
from threading import Lock
from functools import reduce
import socket
import os
import time
import re

class MyIpc:    
    RECEIVE_BUFFER_LENGTH = 10240
    MAIN_THREAD_WAIT_INTERVAL = 1
    def __init__(self, serverPort, clientPort, receiveCallback = None):
        if receiveCallback != None:
            self.receiveCallback = receiveCallback
        else:
            self.receiveCallback = None

        self.serverPort = serverPort
        self.port = clientPort
        self.comunicationLock = Lock()
        self.running = False
        start_new_thread(self.start, ())

    def send(self, data:str):
        result = False
        try:
            self.comunicationLock.acquire()
            s = socket.socket()
            s.connect(('127.0.0.1', int(self.serverPort)))
            s.send(data.encode())
            s.close()
            result = True
        except Exception as e:
            print(e)
        finally:
            self.comunicationLock.release()
        return result

    def start(self):
        if self.running:
            return
        self.running = True
        try:
            s = socket.socket()
            s.bind(('', int(self.port)))
            s.listen(5)
            while self.running:
                c, addr = s.accept()
                if self.receiveCallback != None:
                    self.receiveCallback(self, c.recv(MyIpc.RECEIVE_BUFFER_LENGTH).decode())
                pass
        except Exception as e:
            print(e)
            pass

    def kill(self):
        self.running = False
    
    def set_control(self, name, controlType, attributes):
        data = f"create-control {controlType} {name};"
        attributes = [f"change-control {name} 'set-{i}={attributes[i]}';'" for i in attributes]
        data += reduce(lambda  a, b: f'{a} {b}', attributes)
        self.send(data)