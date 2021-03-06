Prefix NVARCHAR2(256);
TmpPlusses NVARCHAR2(256);
PrefixLength NUMBER(10);
QueryizedPrefix NVARCHAR2(2000);
Query VARCHAR2(2000);
MaxSuffixNum NUMBER(10);
MinSuffixLength NUMBER(10);
MaxSuffixLength NUMBER(10);
NewNum NUMBER(10);
NewNumFixLen NVARCHAR2(256);
  
BEGIN

  -- This procedure returns the next available code based on the given code format and the existing records in the database.
  -- Supported format allows any prefix with a generated numerical suffix.

  -- Possible format types with examples (CodeFormat => NewCode):

  -- A) If the given format ends with at least one "+", the new code will have the given prefix and the plus signs will be replaced with the next available number.
  --    If multiple plus signs are at the end, they define minimum len of suffix part (i.e. next available number prefixed with required number of "0").
  -- Examples:
  -- "ab+" => "ab1"
  -- "ab+" => "ab2"
  -- "ab+" => "ab3"
  -- "ab++++" => "ab0004"
  -- "c+" => "c1"
  -- "+" => "1"
  -- "+" => "2"
  -- Note: new code will maintain the length of the existing codes. For example, if the existing records are those above (last one for "ab" prefix "ab0004"):
  -- "ab+" => "ab0005"

  -- B) If the format doesn't end with "+", it is assumes the new code is explicitly defined.
  -- Examples:
  -- "123" => "123"
  -- "abc" => "abc"
  -- "" => ""

  -- C) If an unsupported format is given, the procedure will raise an error:
  -- Examples:
  -- "+123"

  -- Filter parameter:
  -- Filter is used in a case when the code is not unique in the table/view, but is unique within a certain group.
  -- For example, if the table contains column Family, and the codes are generated starting from 1 for each family,
  -- then when inserting a record in Family "X" the procedure should be called with the filter "Family = 'X'".

  --===================================================================
  -- If not using automatically generated code, return the given explicit code:
  
  IF INSTR(CodeFormat, '+') = 0 THEN
    NewCode := CodeFormat;
    RETURN;
  END IF;

  
  --===================================================================
  -- Extract the prefix and calculate minimum length of numeric suffix 
  --		(defined by number of "+") at the end.
  MinSuffixLength := LENGTH( CodeFormat ) - INSTR(CodeFormat, '+') + 1
  Prefix := SUBSTR( CodeFormat, 0, LENGTH( CodeFormat ) - MinSuffixLength );
  
  -- TODO: replace this part with something like REPLICATE('3', MinSuffixLength)
  TmpPlusses := '+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++'
  
  IF SUBSTR(CodeFormat, LENGTH( CodeFormat ) - MinSuffixLength, MinSuffixLength) <> SUBSTR(TmpPlusses, MinSuffixLength) THEN
    RAISE_APPLICATION_ERROR(-20000, 'Invalid code is entered: The value must end with at least only one "+" character, at the end of the code (AutoCode). No "+" are allowed before those at the end.', TRUE);
    RETURN;
  END IF;
  
  
  --===================================================================
  -- Find out the maximal numerical suffix in the existing records:
  
  QueryizedPrefix := '''' || REPLACE( Prefix, '''', '''''' ) || '''';
  
  PrefixLength := LENGTH('x' || Prefix) - 1; -- 'x' fixes zero length string.
  
  Query :=
  'SELECT * FROM
  (
    SELECT
        TO_NUMBER(SUBSTR( ' || ColumnName || ', ' || to_char(PrefixLength + 1) || ')) AS Num,
        LENGTH( ' || ColumnName || ' ) - ' || to_char(PrefixLength) || ' AS Length
    FROM
        ' || TableOrView || '
    WHERE
        ' || ColumnName || ' LIKE ' || QueryizedPrefix || ' || ''%''
        AND TRANSLATE(SUBSTR( ' || ColumnName || ', ' || to_char(PrefixLength + 1) || '),'' 0123456789'','' '') IS NULL'
      || CASE WHEN Filter IS NOT NULL THEN '
        AND ( ' || Filter || ' )' ELSE '' END
      || '
    ORDER BY
        1 DESC, -- Find maximal numeric suffix.
        2 DESC -- If there are more than one suffixes with same value, take the longest code.
  )
    WHERE ROWNUM <= 1';
  
  BEGIN
	 EXECUTE IMMEDIATE Query INTO MaxSuffixNum, MaxSuffixLength;
  EXCEPTION
	 WHEN NO_DATA_FOUND THEN
		MaxSuffixNum := NULL;
  END;
 
  -- If there are no old codes, numbering will start from 1 (single digit)
  IF MaxSuffixNum IS NULL THEN
    MaxSuffixNum := 0;
    MaxSuffixLength := 1;
  END IF;
  
  
  --===================================================================
  -- Compute next available code:
  
  IF MaxSuffixLength < MinSuffixLength THEN
    SET MaxSuffixLength := MinSuffixLength
  END IF;
        
  NewNum := MaxSuffixNum+1;
  
  NewNumFixLen := CAST(NewNum AS NVARCHAR2);
  
  IF LENGTH(NewNumFixLen) < MaxSuffixLength THEN
    NewNumFixLen := RPAD('0', MaxSuffixLength - LENGTH(NewNumFixLen), '0') || NewNumFixLen;
  END IF;
  
  NewCode := Prefix || NewNumFixLen;
  
END;