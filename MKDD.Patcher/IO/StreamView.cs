﻿using System;
using System.IO;

namespace MKDD.Patcher.IO
{
    public class StreamView : Stream
    {
        private readonly Stream mStream;
        private long mSourcePositionCopy;
        private readonly long mStreamPosition;
        private long mPosition;
        private long mLength;
        private readonly long mMaxLength;

        public StreamView( Stream source, long position, long length )
        {
            if ( source == null )
                throw new ArgumentNullException( nameof( source ) );

            if ( position < 0 || position > source.Length || ( position + length ) > source.Length )
                throw new ArgumentOutOfRangeException( nameof( position ) );

            if ( length < 0 )
                throw new ArgumentOutOfRangeException( nameof( length ) );

            mStream = source;
            mStreamPosition = position;
            mPosition = 0;
            mMaxLength = mLength = length;
        }

        public override void Flush()
        {
            mStream.Flush();
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            switch ( origin )
            {
                case SeekOrigin.Begin:
                    {
                        if ( offset > mLength || offset > mStream.Length )
                            throw new ArgumentOutOfRangeException( nameof( offset ) );

                        mPosition = offset;
                    }
                    break;
                case SeekOrigin.Current:
                    {
                        if ( ( mPosition + offset ) > mLength || ( mPosition + offset ) > mStream.Length )
                            throw new ArgumentOutOfRangeException( nameof( offset ) );

                        mPosition += offset;
                    }
                    break;
                case SeekOrigin.End:
                    {
                        if ( offset < mLength || offset > 0 )
                            throw new ArgumentOutOfRangeException( nameof( offset ) );

                        mPosition = ( mStreamPosition + mLength ) - offset;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException( nameof( origin ) );
            }

            return mPosition;
        }

        public override void SetLength( long value )
        {
            if ( value < 0 )
                throw new ArgumentOutOfRangeException( nameof( value ) );

            if ( value > mStream.Length )
                throw new ArgumentOutOfRangeException( nameof( value ) );

            mLength = value;
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            if ( EndOfStream )
                return 0;

            if ( ( mPosition + count ) > mLength )
                count = ( int )( mLength - mPosition );

            PushPosition();
            SetUnderlyingStreamPosition();
            int result = mStream.Read( buffer, offset, count );
            mPosition += count;
            PopPosition();

            return result;
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            if ( EndOfStream )
                throw new IOException( "Attempted to write past end of stream" );

            if ( ( mPosition + count ) > mLength )
                throw new IOException( "Attempted to write past end of stream" );

            PushPosition();
            SetUnderlyingStreamPosition();
            mStream.Write( buffer, offset, count );
            mPosition += count;
            PopPosition();
        }

        public override bool CanRead
        {
            get { return mStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return mStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return mStream.CanWrite; }
        }

        public override long Length
        {
            get { return mLength; }
        }

        public override long Position
        {
            get { return mPosition; }
            set { mPosition = value; }
        }

        public bool EndOfStream => mPosition == mMaxLength;

        public override int ReadByte()
        {
            if ( EndOfStream )
                return -1;

            PushPosition();
            SetUnderlyingStreamPosition();
            int value = mStream.ReadByte();
            mPosition++;
            PopPosition();

            return value;
        }

        public override void WriteByte( byte value )
        {
            if ( EndOfStream )
                throw new IOException( "Attempted to write past end of stream" );

            PushPosition();
            SetUnderlyingStreamPosition();
            mStream.WriteByte( value );
            mPosition++;
            PopPosition();
        }

        protected void PushPosition()
        {
            mSourcePositionCopy = mStream.Position;
        }

        protected void SetUnderlyingStreamPosition()
        {
            mStream.Position = ( mStreamPosition + mPosition );
        }

        protected void PopPosition()
        {
            mStream.Position = mSourcePositionCopy;
        }
    }
}
