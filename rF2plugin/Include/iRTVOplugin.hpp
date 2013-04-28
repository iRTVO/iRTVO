//‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹‹
//›                                                                         ﬁ
//› Module: Internals Example Header File                                   ﬁ
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

#ifndef _INTERNALS_IRTVOPLUGIN_H
#define _INTERNALS_IRTVOPLUGIN_H

#include <windows.h>
#include "InternalsPlugin.hpp"

#define MMAPOUTFILENAME "iRTVOrFactorOut"
#define MMAPINFILENAME "iRTVOrFactorIn"
#define MMAPOUTFILESIZE 1024000
#define MMAPINFILESIZE 1024
#define MAX_DRIVERS 64

// This is used for the app to use the plugin for its intended purpose
class iRTVOplugin : public InternalsPluginV03
{
	public:

	// Constructor/destructor
	iRTVOplugin() {}
	~iRTVOplugin() {}

	// These are the functions derived from base class InternalsPlugin
	// that can be implemented.
	void Startup( long version );  // game startup
	void Shutdown();               // game shutdown

	void EnterRealtime();          // entering realtime
	void ExitRealtime();           // exiting realtime

	void StartSession();           // session has started
	void EndSession();             // session has ended

	// GAME OUTPUT
	long WantsTelemetryUpdates() { return( 0 ); } // CHANGE TO 1 TO ENABLE TELEMETRY EXAMPLE!
	void UpdateTelemetry( const TelemInfoV01 &info );

	bool WantsGraphicsUpdates() { return( true ); } // CHANGE TO TRUE TO ENABLE GRAPHICS EXAMPLE!
	void UpdateGraphics( const GraphicsInfoV02 &info );

	// GAME INPUT
	bool HasHardwareInputs() { return( false ); } // CHANGE TO TRUE TO ENABLE HARDWARE EXAMPLE!
	void UpdateHardware( const float fDT ) { } // update the hardware with the time between frames
	void EnableHardware() { }             // message from game to enable hardware
	void DisableHardware() { }           // message from game to disable hardware

	// See if the plugin wants to take over a hardware control.  If the plugin takes over the
	// control, this method returns true and sets the value of the float pointed to by the
	// second arg.  Otherwise, it returns false and leaves the float unmodified.
	bool CheckHWControl( const char * const controlName, float &fRetVal );

	bool ForceFeedback( float &forceValue );  // SEE FUNCTION BODY TO ENABLE FORCE EXAMPLE

	// SCORING OUTPUT
	bool WantsScoringUpdates() { return( true ); } // CHANGE TO TRUE TO ENABLE SCORING EXAMPLE!
	void UpdateScoring( const ScoringInfoV01 &info );

	// COMMENTARY INPUT
	bool RequestCommentary( CommentaryRequestInfoV01 &info );  // SEE FUNCTION BODY TO ENABLE COMMENTARY EXAMPLE

	// VIDEO EXPORT (sorry, no example code at this time)
	virtual bool WantsVideoOutput() { return( false ); }         // whether we want to export video
	virtual bool VideoOpen( const char * const szFilename, float fQuality, unsigned short usFPS, unsigned long fBPS,
							unsigned short usWidth, unsigned short usHeight, char *cpCodec = NULL ) { return( false ); } // open video output file
	virtual void VideoClose() {}                                 // close video output file
	virtual void VideoWriteAudio( const short *pAudio, unsigned int uNumFrames ) {} // write some audio info
	virtual void VideoWriteImage( const unsigned char *pImage ) {} // write video image

	// CAMERA CONTROL
	bool WantsToViewVehicle( CameraControlInfoV01 &camControl );

	private:
	// memory mapped file
	TCHAR* MMapOutFileName;
	HANDLE MMapOutFile;
	TCHAR* MMapOutBuf;

	TCHAR* MMapInFileName;
	HANDLE MMapInFile;
	TCHAR* MMapInBuf;

	// timestamp
	unsigned int timestamp_out;
	unsigned int timestamp_in;

	// cameras
	long camera;
	long followedDriver;

	// drivers
	int findDriverByName(char name[32], long mID);
	int findDriverById(long mID);

	char driverNames[MAX_DRIVERS][32];
	long drivermIDs[MAX_DRIVERS];
	int driverCount;

	// input struct
	#pragma pack(4)
	struct CameraRequest
	{
		public:
			unsigned int timestamp;
			unsigned int caridx;
			unsigned int cameraid;
	};

	// Debug
	FILE *debugFile;

};

#endif // _INTERNALS_IRTVOPLUGIN_H

