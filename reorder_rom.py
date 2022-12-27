from os.path import exists

def main():
    path = "ROM_MicrocodeALUFlags"
    if not exists(path):
        raise Exception("File does not exist")

    oldBytes = bytes()
    with open(path, mode='rb') as file:
        oldBytes = file.read()

    newBytes = bytearray()
    for byte in oldBytes:
        bits = [(byte >> i) & 1 for i in range(0, 8)]
        newBits = bits[2:5] + bits[0:2] + bits[5:8]
        newByte = 0
        for i in range(0, 8):
            newByte |= (newBits[i] << i)
        newBytes.append(newByte)

    newPath = path + "_new"
    with open(newPath, mode='wb') as newFile:
        newFile.write(newBytes)

if __name__ == "__main__":
    main()