//‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹
//›                                                                         ﬁ
//› Module: Internals Example Source File                                   ﬁ
//›                                                                         ﬁ
//› Description: Declarations for the Internals Example Plugin              ﬁ
//›                                                                         ﬁ
//›                                                                         ﬁ
//› This source code module, and all information, data, and algorithms      ﬁ
//› associated with it, are part of CUBE technology (tm).                   ﬁ
//›                 PROPRIETARY AND CONFIDENTIAL                            ﬁ
//› Copyright (c) 1996-2008 Image Space Incorporated.  All rights reserved. ﬁ
//›                                                                         ﬁ
//›                                                                         ﬁ
//› Change history:                                                         ﬁ
//›   tag.2005.11.30: created                                               ﬁ
//›                                                                         ﬁ
//ﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂﬂ

#include <stdio.h>
#include "iRTVOplugin.hpp"
#include "cJSON.h"

// plugin information
extern "C" __declspec( dllexport )
const char * __cdecl GetPluginName()                   { return( "iRTVOplugin - 0.1" ); }

extern "C" __declspec( dllexport )
PluginObjectType __cdecl GetPluginType()               { return( PO_INTERNALS ); }

extern "C" __declspec( dllexport )
int __cdecl GetPluginVersion()                         { return( 3 ); } // InternalsPluginV03 functionality

extern "C" __declspec( dllexport )
PluginObject * __cdecl CreatePluginObject()            { return( (PluginObject *) new iRTVOplugin ); }

extern "C" __declspec( dllexport )
void __cdecl DestroyPluginObject( PluginObject *obj )  { delete( (iRTVOplugin *) obj ); }

void iRTVOplugin::Startup( long version )
{
	// Executed when rFactor is started
	timestamp_out = 0;
	timestamp_in = 0;
	driverCount = 0;

	// Allocate memory mapped file
	MMapOutFileName = TEXT(MMAPOUTFILENAME);
	MMapOutFile = CreateFileMapping(
				INVALID_HANDLE_VALUE,    // use paging file
				NULL,                    // default security
				PAGE_READWRITE,          // read/write access
				0,                       // maximum object size (high-order DWORD)
				MMAPOUTFILESIZE,			 // maximum object size (low-order DWORD)
				MMapOutFileName);           // name of mapping object

	if (MMapOutFile != NULL)
	{
		MMapOutBuf = (TCHAR*) MapViewOfFile(MMapOutFile,	// handle to map object
						FILE_MAP_WRITE,		// write permission
						0,
						0,
						sizeof(int));
		
		if (MMapOutBuf == NULL)
		{
			CloseHandle(MMapOutFile);
		}
	}

	MMapInFileName = TEXT(MMAPINFILENAME);
	MMapInFile = CreateFileMapping(
				INVALID_HANDLE_VALUE,    // use paging file
				NULL,                    // default security
				PAGE_READWRITE,          // read/write access
				0,                       // maximum object size (high-order DWORD)
				MMAPINFILESIZE,			 // maximum object size (low-order DWORD)
				MMapInFileName);           // name of mapping object

	if (MMapInFile != NULL)
	{
		MMapInBuf = (TCHAR*) MapViewOfFile(MMapInFile,	// handle to map object
						FILE_MAP_READ,		// read permission
						0,
						0,
						sizeof(int));
		
		if (MMapInBuf == NULL)
		{
			CloseHandle(MMapInFile);
		}
	}

	// debug
	debugFile = fopen ("c:\\temp\\rf2.log","w");
}

void iRTVOplugin::Shutdown()
{
	// Destroy memory mapped file
	UnmapViewOfFile(MMapOutBuf);
	CloseHandle(MMapOutFile);

	UnmapViewOfFile(MMapInBuf);
	CloseHandle(MMapInFile);
	
	// debug
	fclose(debugFile);
}

void iRTVOplugin::StartSession()
{
	// Executed when joining server
}

void iRTVOplugin::EndSession()
{
	// Executed when quitting server
}

void iRTVOplugin::EnterRealtime()
{
	// Executed when entering track
}

void iRTVOplugin::ExitRealtime()
{
	// Executed when returning to pits
}

void iRTVOplugin::UpdateTelemetry( const TelemInfoV01 &info )
{
	// Live telemetry from my vehicle
}

void iRTVOplugin::UpdateGraphics( const GraphicsInfoV02 &info )
{
	// camera data

	bool update = 0;

	if(camera != info.mCameraType) {
		fprintf (debugFile, "mCameraType: %i\n", info.mCameraType);
		camera = info.mCameraType;
		update = true;
	}
	
	if(followedDriver != info.mID) {
		fprintf (debugFile, "mID: %i\n", info.mID);
		followedDriver = info.mID;
		update = true;
	}

	if(update) {
		
		if (MMapOutBuf != NULL) {
			cJSON *root;
			root=cJSON_CreateObject();	
			cJSON_AddStringToObject(root, "DataType", "camera");
			cJSON_AddNumberToObject(root, "CameraId", camera);
			cJSON_AddNumberToObject(root, "Followed", findDriverById(followedDriver));
			// write
			TCHAR *rendered = cJSON_Print(root);
			int outputsize = strlen(rendered) * sizeof(TCHAR);
			timestamp_out++;
			CopyMemory((PVOID)(MMapOutBuf+sizeof(int)), &outputsize, sizeof(int));
			CopyMemory((PVOID)(MMapOutBuf+(2*sizeof(int))), rendered, outputsize);
			CopyMemory((PVOID)(MMapOutBuf), &timestamp_out, sizeof(int));

			// cleanup
			free(rendered);
			cJSON_Delete(root);
		}
		
		fflush(debugFile);
	}
}

bool iRTVOplugin::CheckHWControl( const char * const controlName, float &fRetVal )
{
	return false;
}


bool iRTVOplugin::ForceFeedback( float &forceValue )
{
	return false;
}

void iRTVOplugin::UpdateScoring( const ScoringInfoV01 &info )
{
	// Timing and scoring...
	if (MMapOutBuf != NULL) {
		cJSON *root;
		root=cJSON_CreateObject();	
		cJSON_AddStringToObject(root, "DataType", "standings");
		cJSON *session;
		cJSON_AddItemToObject(root, "Session", session=cJSON_CreateObject());
		cJSON_AddStringToObject(session, "TrackName", info.mTrackName);
		cJSON_AddNumberToObject(session, "SessionType", info.mSession); // (0=testday 1-4=practice 5-8=qual 9=warmup 10-13=race)
		cJSON_AddNumberToObject(session, "SessionState", info.mGamePhase);
		/*
			// 0 Before session has begun
			// 1 Reconnaissance laps (race only)
			// 2 Grid walk-through (race only)
			// 3 Formation lap (race only)
			// 4 Starting-light countdown has begun (race only)
			// 5 Green flag
			// 6 Full course yellow / safety car
			// 7 Session stopped
			// 8 Session over
		*/
		cJSON_AddNumberToObject(session, "SessionTime", info.mCurrentET);
		cJSON_AddNumberToObject(session, "SessionLength", info.mEndET);
		cJSON_AddNumberToObject(session, "SessionLaps", info.mMaxLaps);
		cJSON_AddNumberToObject(session, "Flag", info.mYellowFlagState);

		// test these
		//cJSON_AddNumberToObject(session, "StartLights", info.mNumRedLights/info.mStartLight);

		/*
		if(info.mInRealtime)
			cJSON_AddBoolToObject(session, "InReplay", false);
		else
			cJSON_AddBoolToObject(session, "InReplay", true);
		*/
		cJSON_AddNumberToObject(session, "Temperature", info.mAmbientTemp);
		cJSON_AddNumberToObject(session, "TrackTemperature", info.mTrackTemp);

		cJSON *drivers;
		cJSON_AddItemToObject(root, "Drivers", drivers = cJSON_CreateArray());
		for(long i = 0; i < info.mNumVehicles; ++i)
		{
			VehicleScoringInfoV01 &vinfo = info.mVehicle[i];
			int driverid = findDriverByName(vinfo.mDriverName, vinfo.mID);

			cJSON *driver;
			cJSON_AddItemToArray(drivers, driver=cJSON_CreateObject());
			cJSON_AddNumberToObject(driver, "id", driverid);
			cJSON_AddStringToObject(driver, "Name", vinfo.mDriverName);
			cJSON_AddNumberToObject(driver, "LapsCompleted", vinfo.mTotalLaps);
			cJSON_AddNumberToObject(driver, "TrackPct", abs(vinfo.mLapDist/info.mLapDist));
			cJSON_AddNumberToObject(driver, "Previouslap", vinfo.mLastLapTime);
			cJSON_AddNumberToObject(driver, "FastestLap", vinfo.mBestLapTime);
			cJSON_AddNumberToObject(driver, "CurrentLapBegin", vinfo.mLapStartET);
			cJSON_AddNumberToObject(driver, "PitStops", vinfo.mNumPitstops);
			cJSON_AddBoolToObject(driver, "InPits", vinfo.mInPits);
			if(vinfo.mPitState == 3)
				cJSON_AddBoolToObject(driver, "StoppedInPits", true);
			else
				cJSON_AddBoolToObject(driver, "StoppedInPits", false);
			cJSON_AddNumberToObject(driver, "Position", vinfo.mPlace);
		}

		// write
		TCHAR *rendered = cJSON_Print(root);
		int outputsize = strlen(rendered) * sizeof(TCHAR);
		timestamp_out++;
		CopyMemory((PVOID)(MMapOutBuf+sizeof(int)), &outputsize, sizeof(int));
		CopyMemory((PVOID)(MMapOutBuf+(2*sizeof(int))), rendered, outputsize);
		CopyMemory((PVOID)(MMapOutBuf), &timestamp_out, sizeof(int));

		// cleanup
		free(rendered);
		cJSON_Delete(root);

		/*
		for(int i=0; i < driverCount; i++) {
			fprintf (debugFile, "driver[%i]: %s\n", i, driverNames[i]);
		}
		fflush(debugFile);
		*/
	}
}
	

bool iRTVOplugin::RequestCommentary( CommentaryRequestInfoV01 &info )
{
	// Spectator info(?)
	// COMMENT OUT TO ENABLE EXAMPLE
	return( false );

	/*
	// Note: function is called twice per second

	// Say green flag event for no particular reason every 20 seconds ...
	const float timeMod20 = fmodf( mET, 20.0f );
	if( timeMod20 > 19.0f )
	{
	strcpy( info.mName, "GreenFlag" );
	info.mInput1 = 0.0;
	info.mInput2 = 0.0;
	info.mInput3 = 0.0;
	info.mSkipChecks = true;
	return( true );
	}
	*/
	return( false );
}

 bool iRTVOplugin::WantsToViewVehicle( CameraControlInfoV01 &camControl ) {
	// change camera
	if (MMapInBuf != NULL) {
		CameraRequest req;
		memcpy(&req, MMapInBuf, sizeof(CameraRequest));
		if(req.timestamp != timestamp_in) {
			fprintf (debugFile, "ts: %i car: %i camera: %i\n", req.timestamp, req.caridx, req.cameraid);
			fflush(debugFile);
			timestamp_in = req.timestamp;
			if(req.caridx != camControl.mID || req.cameraid != camControl.mCameraType) {
				camControl.mID = req.caridx;
				camControl.mCameraType = req.cameraid;
				return true;
			}
			else
				return false;
		}
		return false;
	}
	return false;
 }

int iRTVOplugin::findDriverByName(char name[32], long mID) {

	for(int i = 0; i < MAX_DRIVERS; i++) {
		if(strcmp(driverNames[i], name) == 0)
			return i;
	}

	// add if not found
	drivermIDs[driverCount] = mID;
	strcpy(driverNames[driverCount], name);
	driverCount++;
	return driverCount-1;
}

int iRTVOplugin::findDriverById(long mID) {
	if(mID > 0) {
		for(int i = 0; i < MAX_DRIVERS; i++) {
			if(drivermIDs[i] == mID)
				return i;
		}
	}
	return -1;
}