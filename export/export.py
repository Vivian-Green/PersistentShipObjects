import os
import json
import shutil
import zipfile

# todo: SO MUCH fucking cleanup pls actually clean this up viv, someone might actually look at this one-
#       if you are someone and this is still here - fuck, sorry
#                                                                                                   -viv

# todo: CONSTS ARE LOUD
path = os.getcwd()
rootFolder = os.path.abspath(os.path.join(path, os.pardir))
export_folder_path = os.path.join(rootFolder, "VivianGreen-PersistentShipObjects")
thunderstore_mod_folder_path = os.path.join(
    os.path.expandvars("%appdata%"), "Thunderstore Mod Manager", "DataFolder", "LethalCompany",
    "profiles", "emptyDev", "BepInEx", "plugins", "VivianGreen-PersistentShipObjects"
)

manifest_path = os.path.join(rootFolder, "manifest.json")
export_info_path = os.path.join(rootFolder, "export", "exportInfo.json")
changelog_path = os.path.join(export_folder_path, "changelog.md")

VSDLLBuildPath = os.path.join(
    rootFolder, "bin", "Debug", "net6.0", "PersistentShipObjects.dll"
)


def get_new_version(exportInfo):
    version_parts = exportInfo["version_number"].strip().split(".")
    patch_version = int(version_parts[2]) + 1
    version_parts[2] = str(patch_version)
    new_version = ".".join(version_parts)
    return new_version


def update_json_file(file_path, version):
    with open(file_path, "r") as file:
        data = json.load(file)
        data["version_number"] = version

    with open(file_path, "w") as file:
        json.dump(data, file, indent=2)


def copy_files(source, destination):
    shutil.copy(source, destination)
    print("copied " + source + " to " + destination)


def read_file(file_path):
    with open(changelog_path, "r") as file:
        return file.read()


def find_non_default_index(changelogs):
    for i, changelog in enumerate(changelogs):
        if "- no changes provided" not in changelog:
            return i
    return None


def update_changelog(changelog_path, changelogsStr, version):
    changelogs = changelogsStr.split("## ")[1:]

    non_default_index = find_non_default_index(changelogs)

    if non_default_index is None:
        with open(changelog_path, "w") as file:
            file.write(f"{changelogsStr}")

        return version

    flattened_changelogs = changelogs[non_default_index:]
    flattenedChangelogsString = "## " + "## ".join(flattened_changelogs)

    with open(changelog_path, "w") as file:
        file.write(flattenedChangelogsString)

    actual_version = getNewestVersionFromChangelog(
        flattened_changelogs
    )  # Gets the version of the most recent non-empty patch note
    update_json_file(export_info_path, actual_version)

    return actual_version


def getNewestVersionFromChangelog(changelogs):
    version_line = changelogs[0].split(" ")
    actual_version = version_line[0].strip()
    return actual_version


def get_changelog_texts(changelog_path, version):
    print(
        f"Enter changelog text for version {version} (type ! on a new line to finish):"
    )

    bullet_texts = []
    while True:
        user_input = input().strip()
        if user_input == "!":
            break
        bullet_texts.append(user_input)

    if not bullet_texts:
        bullet_texts = ["no changes provided"]

    new_changelog_texts = (
        f"\n## {version}\n" + "".join([f" - {text}\n" for text in bullet_texts]) + "\n"
    )

    content = read_file(changelog_path)

    return f"{new_changelog_texts}{content}"


def delete_bak_files(folder_path):
    try:
        for filename in os.listdir(folder_path):
            if filename.endswith(".bak"):
                file_path = os.path.join(folder_path, filename)
                os.remove(file_path)
                print(f"Deleted: {file_path}")
        print("Deleted .bak files successfully.")
    except Exception as e:
        print(f"Error deleting .bak files: {e}")


def create_zip(source_folder, output_zip):
    try:
        with zipfile.ZipFile(output_zip, "w") as zipf:
            for root, dirs, files in os.walk(source_folder):
                for file in files:
                    file_path = os.path.join(root, file)
                    arcname = os.path.relpath(file_path, source_folder)
                    zipf.write(file_path, arcname=arcname)
        print(f"Successfully created {output_zip}")
    except Exception as e:
        print(f"Error creating zip file: {e}")


def undo_last_change(changelog_texts):
    if len(changelog_texts) >= 3:
        # Remove the last version changelog
        changelog_texts = changelog_texts[:-2]
    else:
        print("Cannot undo further. No changes to undo.")
    return changelog_texts


def merge_changelogs(changelog_texts):
    if len(changelog_texts) >= 2:
        merged_changelog = f"\n## {changelog_texts[0]}\n" + "".join(
            [f" - {text}\n" for text in changelog_texts[1:]]
        )
        changelog_texts = [merged_changelog]
    else:
        print("Cannot merge further. Not enough changelogs available.")
    return changelog_texts


def merge_specific_changelog(changelog_texts, merge_index):
    if 0 < merge_index <= len(changelog_texts):
        merged_changelog = (
            changelog_texts[merge_index - 1] + changelog_texts[merge_index]
        )
        changelog_texts[merge_index - 1] = merged_changelog
        # Remove the merged changelog
        changelog_texts.pop(merge_index)
    else:
        print(
            f"Invalid index. Please provide a number between 1 and {len(changelog_texts)}."
        )
    return changelog_texts


def confirmChangelog(changelog_texts):
    while True:
        print("Changelog Preview:")
        print(changelog_texts)

        print("\n   Options:")
        print("     1. Confirm changelog")
        print("     2. Undo")
        print("     3. Merge")
        print("     4. Merge <int>")

        user_input = input("Enter your choice: ").strip().lower()

        # todo: switch to cases

        if user_input == "1":
            break  # Continue as normal
        elif user_input == "2":
            changelog_texts = undo_last_change(changelog_texts)
        elif user_input.startswith("3"):
            changelog_texts = merge_changelogs(changelog_texts)
        elif user_input.startswith("4"):
            try:
                merge_index = int(user_input.split()[1])
                changelog_texts = merge_specific_changelog(changelog_texts, merge_index)
            except (ValueError, IndexError):
                print(
                    "Invalid input for merge command. Please use 'm/merge <0 < int <= the number of changelogs>'."
                )
        else:
            print("Invalid input. Please choose a valid option.")

    return changelog_texts


def main():
    # Read exportInfo
    with open(export_info_path, "r") as exportConfig:
        exportInfo = json.load(exportConfig)

    new_version = get_new_version(exportInfo)

    # handle changelog stuff
    new_changelog_texts = get_changelog_texts(changelog_path, new_version)
    new_version = update_changelog(changelog_path, new_changelog_texts, new_version)

    # changelog validation & updating
    confirmChangelog(new_changelog_texts)

    # todo: start timer here

    # Update exportInfo.json and manifest.json with the new version
    update_json_file(export_info_path, new_version)
    update_json_file(manifest_path, new_version)

    # Copy PersistentShipObjects.dll
    copy_files(VSDLLBuildPath, export_folder_path)
    copy_files(VSDLLBuildPath, thunderstore_mod_folder_path)

    # Copy manifest.json
    copy_files(manifest_path, export_folder_path)

    # cleanup & export
    delete_bak_files(export_folder_path)

    zip_filename = os.path.join(rootFolder, "VivianGreen-PersistentShipObjects.zip")
    create_zip(export_folder_path, zip_filename)

    print("\n\n\n exported in " + "todo: put timer here" + "s")


if __name__ == "__main__":
    main()